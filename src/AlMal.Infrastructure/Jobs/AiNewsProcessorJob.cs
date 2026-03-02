using AlMal.Application.Interfaces;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that processes unprocessed news articles through the AI service
/// to generate summaries, sentiment analysis, and contextual background.
/// Processes a maximum of 10 articles per run to avoid API rate limits.
/// </summary>
public class AiNewsProcessorJob
{
    private const int BatchSize = 10;

    private readonly IAiAnalysisService _aiService;
    private readonly AlMalDbContext _context;
    private readonly ILogger<AiNewsProcessorJob> _logger;

    public AiNewsProcessorJob(
        IAiAnalysisService aiService,
        AlMalDbContext context,
        ILogger<AiNewsProcessorJob> logger)
    {
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting AI news processor job at {Time:u}", DateTime.UtcNow);

        try
        {
            // Load unprocessed news articles
            var unprocessedArticles = await _context.NewsArticles
                .Where(a => !a.IsProcessed)
                .OrderBy(a => a.PublishedAt)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (unprocessedArticles.Count == 0)
            {
                _logger.LogDebug("No unprocessed news articles found, skipping");
                return;
            }

            _logger.LogInformation("Processing {Count} news articles with AI analyzer", unprocessedArticles.Count);

            var successCount = 0;
            var failCount = 0;

            foreach (var article in unprocessedArticles)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var result = await _aiService.GenerateNewsContextAsync(article.Id, ct);

                    article.Summary = result.Summary;
                    article.ContextData = result.ContextData;
                    article.IsProcessed = true;

                    // Map sentiment string to enum
                    article.Sentiment = result.Sentiment switch
                    {
                        "Positive" => Sentiment.Positive,
                        "Negative" => Sentiment.Negative,
                        _ => Sentiment.Neutral
                    };

                    await _context.SaveChangesAsync(ct);

                    successCount++;
                    _logger.LogDebug(
                        "Processed news article {Id}: {Title} -> Sentiment: {Sentiment}",
                        article.Id,
                        article.TitleAr.Length > 50
                            ? article.TitleAr[..50] + "..."
                            : article.TitleAr,
                        article.Sentiment);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "Failed to process news article {Id}", article.Id);
                    // Continue processing other articles
                }
            }

            _logger.LogInformation(
                "AI news processor completed: {Success} succeeded, {Failed} failed out of {Total}",
                successCount, failCount, unprocessedArticles.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI news processor job was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI news processor job");
            throw;
        }
    }
}
