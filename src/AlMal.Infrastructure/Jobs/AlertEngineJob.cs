using AlMal.Application.Interfaces;
using AlMal.Application.Services;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

/// <summary>
/// Evaluates all active alerts every 30 seconds during market hours.
/// Checks price, volume, index movement, and disclosure conditions.
/// </summary>
public class AlertEngineJob
{
    private readonly AlMalDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<AlertEngineJob> _logger;

    public AlertEngineJob(
        AlMalDbContext context,
        IWhatsAppService whatsAppService,
        ILogger<AlertEngineJob> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!KuwaitMarketHours.IsMarketOpen())
        {
            _logger.LogDebug("Market is closed, skipping alert engine");
            return;
        }

        _logger.LogInformation("Alert engine started at {Time}", KuwaitMarketHours.GetKuwaitTime());

        try
        {
            var activeAlerts = await _context.Alerts
                .Include(a => a.User)
                .Include(a => a.Stock)
                .Where(a => a.IsActive)
                .ToListAsync(ct);

            _logger.LogDebug("Evaluating {Count} active alerts", activeAlerts.Count);

            foreach (var alert in activeAlerts)
            {
                try
                {
                    var triggered = alert.Type switch
                    {
                        AlertType.Price => await EvaluatePriceAlertAsync(alert, ct),
                        AlertType.Volume => await EvaluateVolumeAlertAsync(alert, ct),
                        AlertType.Index => await EvaluateIndexAlertAsync(alert, ct),
                        AlertType.Disclosure => false, // Handled by DisclosureScraperJob
                        _ => false
                    };

                    if (triggered)
                    {
                        _logger.LogInformation(
                            "Alert {AlertId} triggered for user {UserId} (type: {Type})",
                            alert.Id, alert.UserId, alert.Type);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating alert {AlertId}", alert.Id);
                }
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Alert engine completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during alert engine execution");
            throw;
        }
    }

    /// <summary>
    /// Checks if a stock's current price crossed the target value
    /// based on the alert condition (above/below).
    /// </summary>
    private async Task<bool> EvaluatePriceAlertAsync(Alert alert, CancellationToken ct)
    {
        if (alert.Stock == null || !alert.TargetValue.HasValue)
            return false;

        var currentPrice = alert.Stock.LastPrice;
        if (!currentPrice.HasValue)
            return false;

        var targetValue = alert.TargetValue.Value;
        var condition = alert.Condition?.ToLowerInvariant() ?? "above";

        bool triggered = condition switch
        {
            "above" => currentPrice.Value >= targetValue,
            "below" => currentPrice.Value <= targetValue,
            _ => false
        };

        if (!triggered)
            return false;

        // Avoid re-triggering within 1 hour
        if (alert.LastTriggered.HasValue &&
            (DateTime.UtcNow - alert.LastTriggered.Value).TotalHours < 1)
            return false;

        var message = condition == "above"
            ? $"سعر سهم {alert.Stock.NameAr} ({alert.Stock.Symbol}) وصل إلى {currentPrice.Value:F3} د.ك وتجاوز هدفك {targetValue:F3} د.ك"
            : $"سعر سهم {alert.Stock.NameAr} ({alert.Stock.Symbol}) انخفض إلى {currentPrice.Value:F3} د.ك ودون هدفك {targetValue:F3} د.ك";

        await TriggerAlertAsync(alert, message, ct);
        return true;
    }

    /// <summary>
    /// Checks if today's volume exceeds 3x the 30-day average volume.
    /// </summary>
    private async Task<bool> EvaluateVolumeAlertAsync(Alert alert, CancellationToken ct)
    {
        if (alert.Stock == null)
            return false;

        // Avoid re-triggering within 4 hours for volume alerts
        if (alert.LastTriggered.HasValue &&
            (DateTime.UtcNow - alert.LastTriggered.Value).TotalHours < 4)
            return false;

        var today = DateOnly.FromDateTime(KuwaitMarketHours.GetKuwaitTime());
        var thirtyDaysAgo = today.AddDays(-30);

        // Get today's volume
        var todayPrice = await _context.StockPrices
            .AsNoTracking()
            .Where(sp => sp.StockId == alert.Stock.Id && sp.Date == today)
            .FirstOrDefaultAsync(ct);

        if (todayPrice == null)
            return false;

        // Get 30-day average volume (excluding today)
        var avgVolume = await _context.StockPrices
            .AsNoTracking()
            .Where(sp => sp.StockId == alert.Stock.Id
                         && sp.Date >= thirtyDaysAgo
                         && sp.Date < today)
            .AverageAsync(sp => (double?)sp.Volume, ct);

        if (!avgVolume.HasValue || avgVolume.Value <= 0)
            return false;

        var ratio = todayPrice.Volume / avgVolume.Value;
        if (ratio < 3.0)
            return false;

        var message = $"حجم تداول غير اعتيادي لسهم {alert.Stock.NameAr} ({alert.Stock.Symbol})\n" +
                      $"الحجم اليوم: {todayPrice.Volume:N0}\n" +
                      $"المتوسط (30 يوم): {avgVolume.Value:N0}\n" +
                      $"النسبة: {ratio:F1}x";

        await TriggerAlertAsync(alert, message, ct);
        return true;
    }

    /// <summary>
    /// Checks if any market index changed by more than 2% from open.
    /// </summary>
    private async Task<bool> EvaluateIndexAlertAsync(Alert alert, CancellationToken ct)
    {
        // Avoid re-triggering within 2 hours for index alerts
        if (alert.LastTriggered.HasValue &&
            (DateTime.UtcNow - alert.LastTriggered.Value).TotalHours < 2)
            return false;

        var threshold = alert.TargetValue ?? 2.0m;

        var indices = await _context.MarketIndices
            .AsNoTracking()
            .Where(i => i.ChangePercent.HasValue &&
                        (i.ChangePercent.Value > threshold || i.ChangePercent.Value < -threshold))
            .ToListAsync(ct);

        if (indices.Count == 0)
            return false;

        var triggeredIndex = indices.First();
        var direction = triggeredIndex.ChangePercent > 0 ? "ارتفاع" : "انخفاض";
        var message = $"تحرك مؤشر السوق: {direction} بنسبة {triggeredIndex.ChangePercent:F2}%\n" +
                      $"المؤشر: {triggeredIndex.NameAr}\n" +
                      $"القيمة: {triggeredIndex.Value:F2}\n" +
                      $"التغير: {triggeredIndex.Change:F2}";

        await TriggerAlertAsync(alert, message, ct);
        return true;
    }

    /// <summary>
    /// Creates an AlertHistory record, an in-app Notification, optionally sends WhatsApp,
    /// and updates the alert's LastTriggered timestamp.
    /// </summary>
    private async Task TriggerAlertAsync(Alert alert, string message, CancellationToken ct)
    {
        // 1. Create AlertHistory record
        var channel = alert.Channel;
        var alertHistory = new AlertHistory
        {
            AlertId = alert.Id,
            TriggeredAt = DateTime.UtcNow,
            Message = message,
            DeliveryStatus = DeliveryStatus.Pending,
            Channel = channel
        };
        _context.AlertHistories.Add(alertHistory);

        // 2. Create in-app Notification
        var notificationType = alert.Type switch
        {
            AlertType.Price => NotificationType.PriceAlert,
            AlertType.Volume => NotificationType.PriceAlert,
            AlertType.Disclosure => NotificationType.NewDisclosure,
            AlertType.Index => NotificationType.System,
            _ => NotificationType.System
        };

        var notification = new Notification
        {
            UserId = alert.UserId,
            Title = GetAlertTitle(alert.Type),
            Body = message,
            Type = notificationType,
            ReferenceId = alert.Stock?.Symbol,
            IsRead = false
        };
        _context.Notifications.Add(notification);

        // 3. Send WhatsApp if user opted in
        if ((channel == AlertChannel.WhatsApp || channel == AlertChannel.Both)
            && alert.User.WhatsAppOptIn
            && !string.IsNullOrWhiteSpace(alert.User.WhatsAppNumber))
        {
            try
            {
                var parameters = BuildWhatsAppParameters(alert, message);
                await _whatsAppService.SendAlertAsync(
                    alert.User.WhatsAppNumber,
                    alert.Type.ToString().ToLowerInvariant(),
                    parameters);

                alertHistory.DeliveryStatus = DeliveryStatus.Sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp alert for alert {AlertId}", alert.Id);
                alertHistory.DeliveryStatus = DeliveryStatus.Failed;
            }
        }
        else
        {
            alertHistory.DeliveryStatus = DeliveryStatus.Sent;
        }

        // 4. Update LastTriggered
        alert.LastTriggered = DateTime.UtcNow;
    }

    private static string GetAlertTitle(AlertType type) => type switch
    {
        AlertType.Price => "تنبيه سعر",
        AlertType.Volume => "تنبيه حجم تداول",
        AlertType.Disclosure => "إفصاح جديد",
        AlertType.Index => "تحرك المؤشر",
        _ => "تنبيه"
    };

    private static Dictionary<string, string> BuildWhatsAppParameters(Alert alert, string message)
    {
        var parameters = new Dictionary<string, string>
        {
            ["Message"] = message
        };

        if (alert.Stock != null)
        {
            parameters["StockName"] = alert.Stock.NameAr;
            parameters["Symbol"] = alert.Stock.Symbol;
            if (alert.Stock.LastPrice.HasValue)
                parameters["CurrentPrice"] = alert.Stock.LastPrice.Value.ToString("F3");
        }

        if (alert.TargetValue.HasValue)
            parameters["TargetPrice"] = alert.TargetValue.Value.ToString("F3");

        if (!string.IsNullOrEmpty(alert.Condition))
            parameters["Condition"] = alert.Condition == "above" ? "أعلى من" : "أقل من";

        return parameters;
    }
}
