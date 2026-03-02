using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that fetches news articles from an external provider,
/// deduplicates them against existing records, matches them to stocks by keyword,
/// and persists new articles to the database.
/// </summary>
public class NewsFetcherJob
{
    private readonly INewsProvider _newsProvider;
    private readonly AlMalDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<NewsFetcherJob> _logger;

    public NewsFetcherJob(
        INewsProvider newsProvider,
        AlMalDbContext context,
        IDistributedCache cache,
        ILogger<NewsFetcherJob> logger)
    {
        _newsProvider = newsProvider;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting news fetch job at {Time:u}", DateTime.UtcNow);

        try
        {
            // 1. Fetch articles from the external news provider
            var fetchedArticles = await _newsProvider.FetchLatestNewsAsync(ct);
            _logger.LogInformation("Fetched {Count} articles from news provider.", fetchedArticles.Count);

            if (fetchedArticles.Count == 0)
                return;

            // 2. Deduplicate by ExternalId — load existing external IDs from DB
            var externalIds = fetchedArticles
                .Where(a => !string.IsNullOrWhiteSpace(a.ExternalId))
                .Select(a => a.ExternalId!)
                .Distinct()
                .ToList();

            var existingExternalIds = externalIds.Count > 0
                ? await _context.NewsArticles
                    .AsNoTracking()
                    .Where(n => n.ExternalId != null && externalIds.Contains(n.ExternalId))
                    .Select(n => n.ExternalId!)
                    .ToListAsync(ct)
                : [];

            var existingIdSet = new HashSet<string>(existingExternalIds, StringComparer.OrdinalIgnoreCase);

            var newArticles = fetchedArticles
                .Where(a => string.IsNullOrWhiteSpace(a.ExternalId) || !existingIdSet.Contains(a.ExternalId!))
                .ToList();

            _logger.LogInformation("{NewCount} new articles after deduplication (skipped {SkipCount} duplicates).",
                newArticles.Count, fetchedArticles.Count - newArticles.Count);

            if (newArticles.Count == 0)
                return;

            // 3. Load active stocks for keyword matching
            var stocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.Id, s.Symbol, s.NameAr, s.NameEn })
                .ToListAsync(ct);

            var totalMatches = 0;

            foreach (var articleData in newArticles)
            {
                ct.ThrowIfCancellationRequested();

                var newsArticle = new NewsArticle
                {
                    TitleAr = articleData.TitleAr,
                    Source = articleData.Source,
                    SourceUrl = articleData.SourceUrl,
                    Sentiment = Sentiment.Neutral, // Default; AI processing sets this later
                    Summary = null, // AI processing sets this later
                    ContextData = articleData.ContentAr,
                    PublishedAt = articleData.PublishedAt,
                    ExternalId = articleData.ExternalId,
                    ImageUrl = articleData.ImageUrl,
                    IsProcessed = false,
                    CreatedAt = DateTime.UtcNow
                };

                // Match article to stocks by keyword (NameAr or Symbol in TitleAr)
                var title = articleData.TitleAr;
                foreach (var stock in stocks)
                {
                    var matched = false;

                    // Match by Arabic name
                    if (!string.IsNullOrWhiteSpace(stock.NameAr)
                        && title.Contains(stock.NameAr, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                    }

                    // Match by symbol
                    if (!matched
                        && !string.IsNullOrWhiteSpace(stock.Symbol)
                        && title.Contains(stock.Symbol, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                    }

                    // Match by English name (if available)
                    if (!matched
                        && !string.IsNullOrWhiteSpace(stock.NameEn)
                        && title.Contains(stock.NameEn, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                    }

                    if (matched)
                    {
                        newsArticle.NewsArticleStocks.Add(new NewsArticleStock
                        {
                            StockId = stock.Id
                        });
                        totalMatches++;
                    }
                }

                _context.NewsArticles.Add(newsArticle);
            }

            await _context.SaveChangesAsync(ct);

            // Invalidate news cache so fresh data is served
            await _cache.RemoveAsync("news:feed:page:1", ct);

            _logger.LogInformation(
                "News fetch job completed: {Fetched} fetched, {New} new, {Matches} stock matches.",
                fetchedArticles.Count, newArticles.Count, totalMatches);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("News fetch job was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during news fetch job.");
            throw;
        }
    }
}
