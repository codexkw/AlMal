using System.Text.Json;
using AlMal.Application.DTOs.AI;
using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.ExternalApis;

/// <summary>
/// AI analysis service powered by the Claude API (Anthropic).
/// Provides movement explanations, disclosure summaries, and news context generation.
/// </summary>
public class ClaudeAiService : IAiAnalysisService
{
    private const string FallbackMessage = "\u062e\u062f\u0645\u0629 \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064a \u063a\u064a\u0631 \u0645\u062a\u0648\u0641\u0631\u0629 \u062d\u0627\u0644\u064a\u0627\u064b";
    private const string Disclaimer = "\u0647\u0630\u0627 \u062a\u062d\u0644\u064a\u0644 \u062a\u0639\u0644\u064a\u0645\u064a \u0648\u0644\u064a\u0633 \u0646\u0635\u064a\u062d\u0629 \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629";
    private const string ModelId = "claude-sonnet-4-20250514";
    private const int MaxTokens = 2048;
    private const decimal Temperature = 0.3m;
    private static readonly TimeSpan MovementCacheTtl = TimeSpan.FromMinutes(30);

    private readonly IConfiguration _configuration;
    private readonly AlMalDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ClaudeAiService> _logger;

    public ClaudeAiService(
        IConfiguration configuration,
        AlMalDbContext context,
        IDistributedCache cache,
        ILogger<ClaudeAiService> logger)
    {
        _configuration = configuration;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MovementExplanationResult> ExplainMovementAsync(string symbol, CancellationToken ct = default)
    {
        // Check Redis cache first
        var cacheKey = $"ai:movement:{symbol}";
        try
        {
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("Returning cached movement explanation for {Symbol}", symbol);
                var cachedResult = JsonSerializer.Deserialize<MovementExplanationResult>(cached);
                if (cachedResult != null)
                    return cachedResult;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read movement explanation from cache for {Symbol}", symbol);
        }

        // Load stock data from DB
        var stock = await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Symbol == symbol, ct);

        if (stock == null)
        {
            return new MovementExplanationResult
            {
                Explanation = $"\u0644\u0645 \u064a\u062a\u0645 \u0627\u0644\u0639\u062b\u0648\u0631 \u0639\u0644\u0649 \u0633\u0647\u0645 \u0628\u0631\u0645\u0632 {symbol}",
                Disclaimer = Disclaimer
            };
        }

        // Load last 5 days OHLCV
        var recentPrices = await _context.StockPrices
            .AsNoTracking()
            .Where(sp => sp.StockId == stock.Id)
            .OrderByDescending(sp => sp.Date)
            .Take(5)
            .ToListAsync(ct);

        // Load recent disclosures (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentDisclosures = await _context.Disclosures
            .AsNoTracking()
            .Where(d => d.StockId == stock.Id && d.PublishedDate >= thirtyDaysAgo)
            .OrderByDescending(d => d.PublishedDate)
            .Take(5)
            .ToListAsync(ct);

        // Build price data summary
        var priceDataText = recentPrices.Count > 0
            ? string.Join("\n", recentPrices.Select(p =>
                $"  \u0627\u0644\u062a\u0627\u0631\u064a\u062e: {p.Date:yyyy-MM-dd} | \u0627\u0641\u062a\u062a\u0627\u062d: {p.Open:F3} | \u0623\u0639\u0644\u0649: {p.High:F3} | \u0623\u062f\u0646\u0649: {p.Low:F3} | \u0625\u063a\u0644\u0627\u0642: {p.Close:F3} | \u0627\u0644\u062d\u062c\u0645: {p.Volume:N0}"))
            : "  \u0644\u0627 \u062a\u062a\u0648\u0641\u0631 \u0628\u064a\u0627\u0646\u0627\u062a \u0623\u0633\u0639\u0627\u0631 \u062d\u062f\u064a\u062b\u0629";

        // Build disclosure summary
        var disclosureText = recentDisclosures.Count > 0
            ? string.Join("\n", recentDisclosures.Select(d =>
                $"  [{d.PublishedDate:yyyy-MM-dd}] {d.TitleAr}"))
            : "  \u0644\u0627 \u062a\u0648\u062c\u062f \u0625\u0641\u0635\u0627\u062d\u0627\u062a \u062d\u062f\u064a\u062b\u0629";

        var systemPrompt =
            "\u0623\u0646\u062a \u0645\u062d\u0644\u0644 \u0645\u0627\u0644\u064a \u062a\u0639\u0644\u064a\u0645\u064a \u0645\u062a\u062e\u0635\u0635 \u0641\u064a \u0628\u0648\u0631\u0635\u0629 \u0627\u0644\u0643\u0648\u064a\u062a. " +
            "\u0645\u0647\u0645\u062a\u0643 \u0647\u064a \u0634\u0631\u062d \u062a\u062d\u0631\u0643\u0627\u062a \u0627\u0644\u0623\u0633\u0639\u0627\u0631 \u0628\u0623\u0633\u0644\u0648\u0628 \u062a\u0639\u0644\u064a\u0645\u064a \u0645\u0628\u0633\u0637 \u0628\u0627\u0644\u0644\u063a\u0629 \u0627\u0644\u0639\u0631\u0628\u064a\u0629. " +
            "\u0644\u0627 \u062a\u0642\u062f\u0645 \u0646\u0635\u0627\u0626\u062d \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629 \u0623\u0648 \u062a\u0648\u0635\u064a\u0627\u062a \u0628\u0627\u0644\u0634\u0631\u0627\u0621 \u0623\u0648 \u0627\u0644\u0628\u064a\u0639. " +
            "\u0644\u0627 \u062a\u062a\u0646\u0628\u0623 \u0628\u0627\u0644\u0623\u0633\u0639\u0627\u0631 \u0627\u0644\u0645\u0633\u062a\u0642\u0628\u0644\u064a\u0629. " +
            "\u0627\u0634\u0631\u062d \u0641\u0642\u0637 \u0645\u0627 \u062d\u062f\u062b \u0648\u0627\u0644\u0639\u0648\u0627\u0645\u0644 \u0627\u0644\u0645\u0645\u0643\u0646\u0629 \u0627\u0644\u062a\u064a \u0623\u062b\u0631\u062a \u0639\u0644\u0649 \u0627\u0644\u0633\u0639\u0631.";

        var userPrompt =
            $"\u0627\u0634\u0631\u062d \u062a\u062d\u0631\u0643 \u0633\u0639\u0631 \u0633\u0647\u0645 {stock.NameAr} ({stock.Symbol}) \u0641\u064a \u0642\u0637\u0627\u0639 {stock.Sector.NameAr}.\n\n" +
            $"\u0628\u064a\u0627\u0646\u0627\u062a \u0627\u0644\u0623\u0633\u0639\u0627\u0631 (\u0622\u062e\u0631 5 \u0623\u064a\u0627\u0645):\n{priceDataText}\n\n" +
            $"\u0627\u0644\u0625\u0641\u0635\u0627\u062d\u0627\u062a \u0627\u0644\u0623\u062e\u064a\u0631\u0629 (30 \u064a\u0648\u0645):\n{disclosureText}\n\n" +
            $"\u0627\u0644\u0633\u0639\u0631 \u0627\u0644\u062d\u0627\u0644\u064a: {stock.LastPrice?.ToString("F3") ?? "\u063a\u064a\u0631 \u0645\u062a\u0648\u0641\u0631"} | " +
            $"\u0627\u0644\u062a\u063a\u064a\u064a\u0631: {stock.DayChange?.ToString("F3") ?? "0"} ({stock.DayChangePercent?.ToString("F2") ?? "0"}%)\n\n" +
            "\u0642\u062f\u0645 \u0634\u0631\u062d\u0627\u064b \u062a\u0639\u0644\u064a\u0645\u064a\u0627\u064b \u0645\u0648\u062c\u0632\u0627\u064b (\u0641\u0642\u0631\u062a\u064a\u0646 \u0625\u0644\u0649 \u062b\u0644\u0627\u062b \u0641\u0642\u0631\u0627\u062a) \u0639\u0646 \u0623\u0633\u0628\u0627\u0628 \u0647\u0630\u0627 \u0627\u0644\u062a\u062d\u0631\u0643.";

        var explanation = await CallClaudeAsync(systemPrompt, userPrompt, ct);

        var result = new MovementExplanationResult
        {
            Explanation = explanation,
            Disclaimer = Disclaimer
        };

        // Cache result in Redis
        try
        {
            var json = JsonSerializer.Serialize(result);
            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = MovementCacheTtl
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache movement explanation for {Symbol}", symbol);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> SummarizeDisclosureAsync(string disclosureContentAr, CancellationToken ct = default)
    {
        var systemPrompt =
            "\u0623\u0646\u062a \u0645\u062d\u0644\u0644 \u0645\u0627\u0644\u064a \u062a\u0639\u0644\u064a\u0645\u064a \u0645\u062a\u062e\u0635\u0635 \u0641\u064a \u062a\u0644\u062e\u064a\u0635 \u0625\u0641\u0635\u0627\u062d\u0627\u062a \u0627\u0644\u0634\u0631\u0643\u0627\u062a \u0627\u0644\u0645\u062f\u0631\u062c\u0629 \u0641\u064a \u0628\u0648\u0631\u0635\u0629 \u0627\u0644\u0643\u0648\u064a\u062a. " +
            "\u0644\u062e\u0635 \u0627\u0644\u0625\u0641\u0635\u0627\u062d \u0641\u064a 2-3 \u062c\u0645\u0644 \u0628\u0627\u0644\u0639\u0631\u0628\u064a\u0629 \u0645\u0639 \u0627\u0644\u062a\u0631\u0643\u064a\u0632 \u0639\u0644\u0649 \u062a\u0623\u062b\u064a\u0631\u0647 \u0639\u0644\u0649 \u0627\u0644\u0645\u0633\u0627\u0647\u0645\u064a\u0646. " +
            "\u0644\u0627 \u062a\u0642\u062f\u0645 \u0646\u0635\u0627\u0626\u062d \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629.";

        var userPrompt =
            $"\u0644\u062e\u0635 \u0647\u0630\u0627 \u0627\u0644\u0625\u0641\u0635\u0627\u062d \u0641\u064a 2-3 \u062c\u0645\u0644 \u0645\u0639 \u0627\u0644\u062a\u0631\u0643\u064a\u0632 \u0639\u0644\u0649 \u0645\u0627 \u064a\u0639\u0646\u064a\u0647 \u0644\u0644\u0645\u0633\u0627\u0647\u0645\u064a\u0646:\n\n{disclosureContentAr}";

        var summary = await CallClaudeAsync(systemPrompt, userPrompt, ct);

        return $"{summary}\n\n{Disclaimer}";
    }

    /// <inheritdoc />
    public async Task<NewsContextResult> GenerateNewsContextAsync(long newsArticleId, CancellationToken ct = default)
    {
        // Load the target article with related stocks
        var article = await _context.NewsArticles
            .AsNoTracking()
            .Include(a => a.NewsArticleStocks)
                .ThenInclude(nas => nas.Stock)
            .FirstOrDefaultAsync(a => a.Id == newsArticleId, ct);

        if (article == null)
        {
            return new NewsContextResult
            {
                Summary = "\u0644\u0645 \u064a\u062a\u0645 \u0627\u0644\u0639\u062b\u0648\u0631 \u0639\u0644\u0649 \u0627\u0644\u0645\u0642\u0627\u0644",
                Sentiment = "Neutral",
                Disclaimer = Disclaimer
            };
        }

        // Load last 10 related articles for the same stocks (for context)
        var relatedStockIds = article.NewsArticleStocks.Select(nas => nas.StockId).ToList();
        var relatedArticles = new List<string>();

        if (relatedStockIds.Count > 0)
        {
            relatedArticles = await _context.NewsArticleStocks
                .AsNoTracking()
                .Where(nas => relatedStockIds.Contains(nas.StockId)
                              && nas.NewsArticleId != newsArticleId)
                .OrderByDescending(nas => nas.NewsArticle.PublishedAt)
                .Take(10)
                .Select(nas => $"  [{nas.NewsArticle.PublishedAt:yyyy-MM-dd}] {nas.NewsArticle.TitleAr}")
                .ToListAsync(ct);
        }

        var stockNames = article.NewsArticleStocks
            .Select(nas => nas.Stock.NameAr)
            .ToList();

        var relatedArticlesText = relatedArticles.Count > 0
            ? string.Join("\n", relatedArticles)
            : "  \u0644\u0627 \u062a\u0648\u062c\u062f \u0623\u062e\u0628\u0627\u0631 \u0633\u0627\u0628\u0642\u0629 \u0645\u0631\u062a\u0628\u0637\u0629";

        var stocksText = stockNames.Count > 0
            ? string.Join("\u060c ", stockNames)
            : "\u063a\u064a\u0631 \u0645\u062d\u062f\u062f";

        var systemPrompt =
            "\u0623\u0646\u062a \u0645\u062d\u0644\u0644 \u0623\u062e\u0628\u0627\u0631 \u0645\u0627\u0644\u064a\u0629 \u062a\u0639\u0644\u064a\u0645\u064a \u0645\u062a\u062e\u0635\u0635 \u0641\u064a \u0633\u0648\u0642 \u0627\u0644\u0643\u0648\u064a\u062a \u0627\u0644\u0645\u0627\u0644\u064a. " +
            "\u062d\u0644\u0644 \u0627\u0644\u062e\u0628\u0631 \u0648\u0642\u062f\u0645: 1) \u0645\u0644\u062e\u0635\u0627\u064b \u0645\u0648\u062c\u0632\u0627\u064b \u0628\u0627\u0644\u0639\u0631\u0628\u064a\u0629\u060c 2) \u062a\u0642\u064a\u064a\u0645 \u0627\u0644\u0645\u0634\u0627\u0639\u0631 (\u0625\u064a\u062c\u0627\u0628\u064a/\u0633\u0644\u0628\u064a/\u0645\u062d\u0627\u064a\u062f)\u060c 3) \u0633\u064a\u0627\u0642 \u062e\u0644\u0641\u064a \u0625\u0630\u0627 \u062a\u0648\u0641\u0631. " +
            "\u0644\u0627 \u062a\u0642\u062f\u0645 \u0646\u0635\u0627\u0626\u062d \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629 \u0623\u0648 \u062a\u0648\u0642\u0639\u0627\u062a \u0633\u0639\u0631\u064a\u0629. " +
            "\u0623\u062c\u0628 \u0628\u0627\u0644\u0635\u064a\u063a\u0629 \u0627\u0644\u062a\u0627\u0644\u064a\u0629 \u0628\u0627\u0644\u0636\u0628\u0637:\n" +
            "[\u0627\u0644\u0645\u0644\u062e\u0635]\n<\u0627\u0644\u0645\u0644\u062e\u0635 \u0647\u0646\u0627>\n[\u0627\u0644\u0645\u0634\u0627\u0639\u0631]\n<Positive \u0623\u0648 Negative \u0623\u0648 Neutral>\n[\u0627\u0644\u0633\u064a\u0627\u0642]\n<\u0627\u0644\u0633\u064a\u0627\u0642 \u0627\u0644\u062e\u0644\u0641\u064a \u0647\u0646\u0627>";

        var userPrompt =
            $"\u062d\u0644\u0644 \u0647\u0630\u0627 \u0627\u0644\u062e\u0628\u0631 \u0627\u0644\u0645\u0627\u0644\u064a:\n\n" +
            $"\u0627\u0644\u0639\u0646\u0648\u0627\u0646: {article.TitleAr}\n" +
            $"\u0627\u0644\u0645\u0635\u062f\u0631: {article.Source}\n" +
            $"\u0627\u0644\u062a\u0627\u0631\u064a\u062e: {article.PublishedAt:yyyy-MM-dd}\n" +
            $"\u0627\u0644\u0634\u0631\u0643\u0627\u062a \u0627\u0644\u0645\u0631\u062a\u0628\u0637\u0629: {stocksText}\n" +
            (article.ContextData != null ? $"\u0627\u0644\u0645\u062d\u062a\u0648\u0649: {article.ContextData}\n" : "") +
            $"\n\u0623\u062e\u0628\u0627\u0631 \u0633\u0627\u0628\u0642\u0629 \u0644\u0644\u0634\u0631\u0643\u0627\u062a \u0630\u0627\u062a\u0647\u0627:\n{relatedArticlesText}";

        var response = await CallClaudeAsync(systemPrompt, userPrompt, ct);

        // Parse the structured response
        var result = ParseNewsContextResponse(response);
        result.Disclaimer = Disclaimer;
        return result;
    }

    /// <inheritdoc />
    public async Task<string> AnswerMarketQuestionAsync(string question, string? userId = null, CancellationToken ct = default)
    {
        // Build market context
        var contextParts = new List<string>();

        // Latest market indices
        var indices = await _context.MarketIndices
            .AsNoTracking()
            .OrderBy(i => i.Type)
            .Take(10)
            .ToListAsync(ct);

        if (indices.Count > 0)
        {
            var indicesText = string.Join("\n", indices.Select(i =>
                $"  {i.NameAr}: {i.Value:F2} (التغير: {i.ChangePercent:F2}%)"));
            contextParts.Add($"مؤشرات السوق:\n{indicesText}");
        }

        // Check if question mentions a specific stock symbol
        var allSymbols = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Symbol, s.NameAr, s.Id })
            .ToListAsync(ct);

        var mentionedStock = allSymbols.FirstOrDefault(s =>
            question.Contains(s.Symbol, StringComparison.OrdinalIgnoreCase) ||
            question.Contains(s.NameAr, StringComparison.Ordinal));

        if (mentionedStock != null)
        {
            var stock = await _context.Stocks
                .AsNoTracking()
                .Include(s => s.Sector)
                .FirstOrDefaultAsync(s => s.Id == mentionedStock.Id, ct);

            if (stock != null)
            {
                contextParts.Add(
                    $"بيانات السهم المذكور:\n" +
                    $"  الاسم: {stock.NameAr} ({stock.Symbol})\n" +
                    $"  القطاع: {stock.Sector.NameAr}\n" +
                    $"  السعر: {stock.LastPrice?.ToString("F3") ?? "غير متوفر"} د.ك\n" +
                    $"  التغير: {stock.DayChange?.ToString("F3") ?? "0"} ({stock.DayChangePercent?.ToString("F2") ?? "0"}%)");

                // Recent prices
                var recentPrices = await _context.StockPrices
                    .AsNoTracking()
                    .Where(sp => sp.StockId == stock.Id)
                    .OrderByDescending(sp => sp.Date)
                    .Take(5)
                    .ToListAsync(ct);

                if (recentPrices.Count > 0)
                {
                    var pricesText = string.Join("\n", recentPrices.Select(p =>
                        $"  {p.Date:yyyy-MM-dd}: إغلاق {p.Close:F3} | حجم {p.Volume:N0}"));
                    contextParts.Add($"أسعار آخر 5 أيام:\n{pricesText}");
                }
            }
        }

        // Recent disclosures (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var recentDisclosures = await _context.Disclosures
            .AsNoTracking()
            .Include(d => d.Stock)
            .Where(d => d.PublishedDate >= sevenDaysAgo)
            .OrderByDescending(d => d.PublishedDate)
            .Take(5)
            .ToListAsync(ct);

        if (recentDisclosures.Count > 0)
        {
            var disclosuresText = string.Join("\n", recentDisclosures.Select(d =>
                $"  [{d.PublishedDate:yyyy-MM-dd}] {d.Stock.NameAr}: {d.TitleAr}"));
            contextParts.Add($"أحدث الإفصاحات:\n{disclosuresText}");
        }

        // Recent news (last 7 days)
        var recentNews = await _context.NewsArticles
            .AsNoTracking()
            .Where(n => n.PublishedAt >= sevenDaysAgo)
            .OrderByDescending(n => n.PublishedAt)
            .Take(5)
            .ToListAsync(ct);

        if (recentNews.Count > 0)
        {
            var newsText = string.Join("\n", recentNews.Select(n =>
                $"  [{n.PublishedAt:yyyy-MM-dd}] {n.TitleAr}"));
            contextParts.Add($"أحدث الأخبار:\n{newsText}");
        }

        // Top gainers/losers
        var topGainers = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent > 0)
            .OrderByDescending(s => s.DayChangePercent)
            .Take(5)
            .ToListAsync(ct);

        var topLosers = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent < 0)
            .OrderBy(s => s.DayChangePercent)
            .Take(5)
            .ToListAsync(ct);

        if (topGainers.Count > 0)
        {
            var gainersText = string.Join("\n", topGainers.Select(s =>
                $"  {s.NameAr} ({s.Symbol}): +{s.DayChangePercent:F2}%"));
            contextParts.Add($"الأسهم الأكثر ارتفاعاً:\n{gainersText}");
        }

        if (topLosers.Count > 0)
        {
            var losersText = string.Join("\n", topLosers.Select(s =>
                $"  {s.NameAr} ({s.Symbol}): {s.DayChangePercent:F2}%"));
            contextParts.Add($"الأسهم الأكثر انخفاضاً:\n{losersText}");
        }

        var contextStr = string.Join("\n\n", contextParts);

        var systemPrompt =
            "أنت مساعد تعليمي متخصص في سوق الكويت المالي (بورصة الكويت). " +
            "تجيب على أسئلة المستخدمين حول السوق والأسهم بأسلوب تعليمي مبسط بالعربية. " +
            "لا تقدم نصائح استثمارية أو توصيات بالشراء أو البيع. " +
            "لا تتنبأ بالأسعار المستقبلية. " +
            "أجب بإيجاز (3-5 فقرات كحد أقصى). " +
            "اختم دائماً بعبارة: هذا تحليل تعليمي وليس نصيحة استثمارية";

        var userPrompt =
            $"سؤال المستخدم: {question}\n\n" +
            $"السياق الحالي للسوق:\n{contextStr}";

        var response = await CallClaudeAsync(systemPrompt, userPrompt, ct);

        // Ensure disclaimer is present
        if (!response.Contains("هذا تحليل تعليمي وليس نصيحة استثمارية"))
        {
            response += "\n\nهذا تحليل تعليمي وليس نصيحة استثمارية";
        }

        return response;
    }

    /// <summary>
    /// Core method to call the Claude API via Anthropic.SDK.
    /// Returns the text response or a fallback message on failure.
    /// </summary>
    private async Task<string> CallClaudeAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var apiKey = _configuration["ExternalApis:Anthropic:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "SET_VIA_ENV_VARIABLE")
        {
            _logger.LogWarning("Anthropic API key is not configured at ExternalApis:Anthropic:ApiKey. Current value: {KeyPrefix}",
                string.IsNullOrWhiteSpace(apiKey) ? "(empty)" : apiKey[..Math.Min(10, apiKey.Length)] + "...");
            return FallbackMessage;
        }

        try
        {
            var client = new AnthropicClient(apiKey);

            var messages = new List<Message>
            {
                new Message(RoleType.User, userPrompt)
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                MaxTokens = MaxTokens,
                Model = ModelId,
                Stream = false,
                Temperature = Temperature,
                System = new List<SystemMessage>
                {
                    new SystemMessage(systemPrompt)
                }
            };

            var response = await client.Messages.GetClaudeMessageAsync(parameters, ct);
            var responseText = response.Message.ToString();

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("Claude API returned empty response");
                return FallbackMessage;
            }

            return responseText.Trim();
        }
        catch (OperationCanceledException)
        {
            throw; // Let cancellation propagate
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API. Model: {Model}, ApiKeyPrefix: {KeyPrefix}",
                ModelId, apiKey[..Math.Min(10, apiKey.Length)] + "...");
            return FallbackMessage;
        }
    }

    /// <summary>
    /// Parses the structured response from the news context prompt into a <see cref="NewsContextResult"/>.
    /// Expected format:
    /// [الملخص]
    /// ...summary text...
    /// [المشاعر]
    /// Positive|Negative|Neutral
    /// [السياق]
    /// ...context text...
    /// </summary>
    private static NewsContextResult ParseNewsContextResponse(string response)
    {
        var result = new NewsContextResult
        {
            Summary = response,
            Sentiment = "Neutral",
            ContextData = null
        };

        // If it's a fallback message, return as-is
        if (response == FallbackMessage)
            return result;

        try
        {
            // Parse structured sections
            var summaryMarker = "[\u0627\u0644\u0645\u0644\u062e\u0635]";
            var sentimentMarker = "[\u0627\u0644\u0645\u0634\u0627\u0639\u0631]";
            var contextMarker = "[\u0627\u0644\u0633\u064a\u0627\u0642]";

            var summaryIdx = response.IndexOf(summaryMarker, StringComparison.Ordinal);
            var sentimentIdx = response.IndexOf(sentimentMarker, StringComparison.Ordinal);
            var contextIdx = response.IndexOf(contextMarker, StringComparison.Ordinal);

            if (summaryIdx >= 0 && sentimentIdx > summaryIdx)
            {
                var summaryStart = summaryIdx + summaryMarker.Length;
                result.Summary = response[summaryStart..sentimentIdx].Trim();
            }

            if (sentimentIdx >= 0)
            {
                var sentimentStart = sentimentIdx + sentimentMarker.Length;
                var sentimentEnd = contextIdx > sentimentIdx ? contextIdx : response.Length;
                var sentimentText = response[sentimentStart..sentimentEnd].Trim();

                // Normalize sentiment
                if (sentimentText.Contains("Positive", StringComparison.OrdinalIgnoreCase)
                    || sentimentText.Contains("\u0625\u064a\u062c\u0627\u0628\u064a", StringComparison.Ordinal))
                {
                    result.Sentiment = "Positive";
                }
                else if (sentimentText.Contains("Negative", StringComparison.OrdinalIgnoreCase)
                         || sentimentText.Contains("\u0633\u0644\u0628\u064a", StringComparison.Ordinal))
                {
                    result.Sentiment = "Negative";
                }
                else
                {
                    result.Sentiment = "Neutral";
                }
            }

            if (contextIdx >= 0)
            {
                var contextStart = contextIdx + contextMarker.Length;
                var contextText = response[contextStart..].Trim();
                if (!string.IsNullOrWhiteSpace(contextText))
                    result.ContextData = contextText;
            }
        }
        catch (Exception)
        {
            // If parsing fails, keep the full response as summary
            result.Summary = response;
            result.Sentiment = "Neutral";
        }

        return result;
    }
}
