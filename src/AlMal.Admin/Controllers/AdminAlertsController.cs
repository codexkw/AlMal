using AlMal.Admin.ViewModels;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Alerts")]
public class AdminAlertsController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<AdminAlertsController> _logger;
    private const int PageSize = 20;

    public AdminAlertsController(AlMalDbContext context, ILogger<AdminAlertsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /Alerts — List all alerts system-wide with user info, status, type filter
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(AlertType? type, string? status, int page = 1)
    {
        if (page < 1) page = 1;

        var query = _context.Alerts
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Stock)
            .AsQueryable();

        // Type filter
        if (type.HasValue)
        {
            query = query.Where(a => a.Type == type.Value);
        }

        // Status filter (active/inactive)
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status == "active")
                query = query.Where(a => a.IsActive);
            else if (status == "inactive")
                query = query.Where(a => !a.IsActive);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page > totalPages && totalPages > 0) page = totalPages;

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(a => new AlertItemViewModel
            {
                Id = a.Id,
                UserId = a.UserId,
                UserDisplayName = a.User.DisplayName,
                UserEmail = a.User.Email,
                Type = a.Type,
                StockSymbol = a.Stock != null ? a.Stock.Symbol : null,
                StockNameAr = a.Stock != null ? a.Stock.NameAr : null,
                Condition = a.Condition,
                TargetValue = a.TargetValue,
                Channel = a.Channel,
                IsActive = a.IsActive,
                LastTriggered = a.LastTriggered,
                CreatedAt = a.CreatedAt,
                TriggerCount = a.AlertHistories.Count
            })
            .ToListAsync();

        // Build delivery stats
        var deliveryStats = await BuildDeliveryStatsAsync();

        var viewModel = new AlertsAdminViewModel
        {
            Alerts = alerts,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            TypeFilter = type,
            StatusFilter = status,
            DeliveryStats = deliveryStats
        };

        return View("~/Views/Alerts/Index.cshtml", viewModel);
    }

    /// <summary>
    /// GET /Alerts/Analytics — Delivery analytics dashboard
    /// </summary>
    [HttpGet("Analytics")]
    public async Task<IActionResult> Analytics()
    {
        var stats = await BuildDeliveryStatsAsync();

        return View("~/Views/Alerts/Analytics.cshtml", stats);
    }

    /// <summary>
    /// POST /Alerts/Deactivate/{id} — Admin deactivate alert
    /// </summary>
    [HttpPost("Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert is null)
        {
            TempData["Error"] = "التنبيه غير موجود";
            return RedirectToAction(nameof(Index));
        }

        alert.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Alert {AlertId} deactivated by admin {Admin}", id, User.Identity?.Name);
        TempData["Success"] = "تم تعطيل التنبيه بنجاح";

        return RedirectToAction(nameof(Index));
    }

    private async Task<AlertDeliveryStatsViewModel> BuildDeliveryStatsAsync()
    {
        var allAlerts = await _context.Alerts
            .AsNoTracking()
            .GroupBy(a => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(a => a.IsActive),
                Inactive = g.Count(a => !a.IsActive),
                AppChannel = g.Count(a => a.Channel == AlertChannel.App),
                WhatsAppChannel = g.Count(a => a.Channel == AlertChannel.WhatsApp),
                BothChannel = g.Count(a => a.Channel == AlertChannel.Both),
                PriceType = g.Count(a => a.Type == AlertType.Price),
                DisclosureType = g.Count(a => a.Type == AlertType.Disclosure),
                VolumeType = g.Count(a => a.Type == AlertType.Volume),
                IndexType = g.Count(a => a.Type == AlertType.Index)
            })
            .FirstOrDefaultAsync();

        var deliveries = await _context.AlertHistories
            .AsNoTracking()
            .GroupBy(h => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Sent = g.Count(h => h.DeliveryStatus == DeliveryStatus.Sent),
                Failed = g.Count(h => h.DeliveryStatus == DeliveryStatus.Failed),
                Pending = g.Count(h => h.DeliveryStatus == DeliveryStatus.Pending)
            })
            .FirstOrDefaultAsync();

        return new AlertDeliveryStatsViewModel
        {
            TotalAlerts = allAlerts?.Total ?? 0,
            ActiveAlerts = allAlerts?.Active ?? 0,
            InactiveAlerts = allAlerts?.Inactive ?? 0,
            AppChannelCount = allAlerts?.AppChannel ?? 0,
            WhatsAppChannelCount = allAlerts?.WhatsAppChannel ?? 0,
            BothChannelCount = allAlerts?.BothChannel ?? 0,
            PriceAlertCount = allAlerts?.PriceType ?? 0,
            DisclosureAlertCount = allAlerts?.DisclosureType ?? 0,
            VolumeAlertCount = allAlerts?.VolumeType ?? 0,
            IndexAlertCount = allAlerts?.IndexType ?? 0,
            TotalDeliveries = deliveries?.Total ?? 0,
            SentCount = deliveries?.Sent ?? 0,
            FailedCount = deliveries?.Failed ?? 0,
            PendingCount = deliveries?.Pending ?? 0
        };
    }
}
