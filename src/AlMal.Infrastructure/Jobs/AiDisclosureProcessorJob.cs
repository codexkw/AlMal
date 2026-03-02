using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job that processes unprocessed disclosures through the AI service
/// to generate Arabic summaries. Processes a maximum of 10 disclosures per run
/// to avoid API rate limits.
/// </summary>
public class AiDisclosureProcessorJob
{
    private const int BatchSize = 10;

    private readonly IAiAnalysisService _aiService;
    private readonly AlMalDbContext _context;
    private readonly ILogger<AiDisclosureProcessorJob> _logger;

    public AiDisclosureProcessorJob(
        IAiAnalysisService aiService,
        AlMalDbContext context,
        ILogger<AiDisclosureProcessorJob> logger)
    {
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting AI disclosure processor job at {Time:u}", DateTime.UtcNow);

        try
        {
            // Load disclosures that have not been processed (AiSummary is null)
            var unprocessedDisclosures = await _context.Disclosures
                .Where(d => d.AiSummary == null)
                .OrderBy(d => d.PublishedDate)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (unprocessedDisclosures.Count == 0)
            {
                _logger.LogDebug("No unprocessed disclosures found, skipping");
                return;
            }

            _logger.LogInformation("Processing {Count} disclosures with AI summarizer", unprocessedDisclosures.Count);

            var successCount = 0;
            var failCount = 0;

            foreach (var disclosure in unprocessedDisclosures)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // Use TitleAr as content for summarization (ContentAr may be null)
                    var content = !string.IsNullOrWhiteSpace(disclosure.ContentAr)
                        ? disclosure.ContentAr
                        : disclosure.TitleAr;

                    var summary = await _aiService.SummarizeDisclosureAsync(content, ct);

                    disclosure.AiSummary = summary;
                    disclosure.IsProcessed = true;

                    await _context.SaveChangesAsync(ct);

                    successCount++;
                    _logger.LogDebug(
                        "Processed disclosure {Id}: {Title}",
                        disclosure.Id,
                        disclosure.TitleAr.Length > 50
                            ? disclosure.TitleAr[..50] + "..."
                            : disclosure.TitleAr);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "Failed to process disclosure {Id}", disclosure.Id);
                    // Continue processing other disclosures
                }
            }

            _logger.LogInformation(
                "AI disclosure processor completed: {Success} succeeded, {Failed} failed out of {Total}",
                successCount, failCount, unprocessedDisclosures.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AI disclosure processor job was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI disclosure processor job");
            throw;
        }
    }
}
