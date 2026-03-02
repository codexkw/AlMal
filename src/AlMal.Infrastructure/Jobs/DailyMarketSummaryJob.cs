using System.Text;
using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

/// <summary>
/// Generates a daily market summary at 12:45 PM KWT (after market close)
/// and sends it to all WhatsApp-opted-in users.
/// Uses Claude AI for intelligent summarization.
/// </summary>
public class DailyMarketSummaryJob
{
    private readonly AlMalDbContext _context;
    private readonly IAiAnalysisService _aiService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<DailyMarketSummaryJob> _logger;

    public DailyMarketSummaryJob(
        AlMalDbContext context,
        IAiAnalysisService aiService,
        IWhatsAppService whatsAppService,
        ILogger<DailyMarketSummaryJob> logger)
    {
        _context = context;
        _aiService = aiService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting daily market summary generation");

        try
        {
            // Build market data for AI summarization
            var summaryData = await BuildMarketDataAsync(ct);

            // Generate AI summary
            var aiSummary = await _aiService.AnswerMarketQuestionAsync(
                $"قم بإعداد ملخص يومي لسوق الكويت المالي بناءً على البيانات التالية:\n{summaryData}\n\n" +
                "اكتب ملخصاً موجزاً ومفيداً يشمل: أداء المؤشرات، أبرز الأسهم الرابحة والخاسرة، وأي إفصاحات مهمة.",
                null, ct);

            // Format the final message
            var message = FormatDailySummary(aiSummary);

            // Get all WhatsApp-opted-in users
            var optedInUsers = await _context.Users
                .AsNoTracking()
                .Where(u => u.WhatsAppOptIn && u.WhatsAppNumber != null && u.IsActive)
                .Select(u => u.WhatsAppNumber!)
                .ToListAsync(ct);

            _logger.LogInformation("Sending daily summary to {Count} WhatsApp users", optedInUsers.Count);

            var sentCount = 0;
            var failCount = 0;

            foreach (var phoneNumber in optedInUsers)
            {
                try
                {
                    await _whatsAppService.SendMessageAsync(phoneNumber, message);
                    sentCount++;

                    // Small delay to avoid rate limiting
                    await Task.Delay(100, ct);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogWarning(ex, "Failed to send daily summary to user");
                }
            }

            _logger.LogInformation(
                "Daily market summary completed. Sent: {Sent}, Failed: {Failed}",
                sentCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily market summary");
            throw;
        }
    }

    /// <summary>
    /// Builds a text representation of today's market data for AI consumption.
    /// </summary>
    private async Task<string> BuildMarketDataAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();

        // Market indices
        var indices = await _context.MarketIndices
            .AsNoTracking()
            .OrderBy(i => i.Type)
            .ToListAsync(ct);

        sb.AppendLine("المؤشرات:");
        foreach (var index in indices)
        {
            sb.AppendLine($"  {index.NameAr}: {index.Value:F2} ({index.ChangePercent:+0.00;-0.00}%)");
        }

        // Top 5 gainers
        var gainers = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent > 0)
            .OrderByDescending(s => s.DayChangePercent)
            .Take(5)
            .ToListAsync(ct);

        sb.AppendLine("\nأكثر 5 أسهم ارتفاعاً:");
        foreach (var s in gainers)
        {
            sb.AppendLine($"  {s.NameAr} ({s.Symbol}): {s.LastPrice:F3} د.ك (+{s.DayChangePercent:F2}%)");
        }

        // Top 5 losers
        var losers = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent < 0)
            .OrderBy(s => s.DayChangePercent)
            .Take(5)
            .ToListAsync(ct);

        sb.AppendLine("\nأكثر 5 أسهم انخفاضاً:");
        foreach (var s in losers)
        {
            sb.AppendLine($"  {s.NameAr} ({s.Symbol}): {s.LastPrice:F3} د.ك ({s.DayChangePercent:F2}%)");
        }

        // Today's disclosures
        var today = DateTime.UtcNow.Date;
        var todayDisclosures = await _context.Disclosures
            .AsNoTracking()
            .Include(d => d.Stock)
            .Where(d => d.PublishedDate >= today)
            .OrderByDescending(d => d.PublishedDate)
            .Take(5)
            .ToListAsync(ct);

        if (todayDisclosures.Count > 0)
        {
            sb.AppendLine("\nإفصاحات اليوم:");
            foreach (var d in todayDisclosures)
            {
                sb.AppendLine($"  {d.Stock.NameAr}: {d.TitleAr}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats the AI-generated summary into a WhatsApp-friendly message.
    /// </summary>
    private static string FormatDailySummary(string aiSummary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\U0001f4ca قناة المال - ملخص السوق اليومي");
        sb.AppendLine("═══════════════════════");
        sb.AppendLine();
        sb.AppendLine(aiSummary);
        sb.AppendLine();
        sb.AppendLine("═══════════════════════");
        sb.AppendLine("هذا تحليل تعليمي وليس نصيحة استثمارية");
        return sb.ToString();
    }
}
