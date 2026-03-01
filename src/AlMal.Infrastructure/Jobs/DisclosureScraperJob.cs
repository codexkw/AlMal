using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

public class DisclosureScraperJob
{
    private readonly IMarketDataProvider _provider;
    private readonly AlMalDbContext _context;
    private readonly ILogger<DisclosureScraperJob> _logger;

    public DisclosureScraperJob(
        IMarketDataProvider provider,
        AlMalDbContext context,
        ILogger<DisclosureScraperJob> logger)
    {
        _provider = provider;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting disclosure scrape");

        try
        {
            var disclosures = await _provider.ScrapeDisclosuresAsync(ct);
            _logger.LogInformation("Scraped {Count} disclosures", disclosures.Count);

            var newCount = 0;
            foreach (var disc in disclosures)
            {
                var stock = await _context.Stocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Symbol == disc.StockSymbol, ct);

                if (stock == null)
                {
                    _logger.LogWarning("Stock {Symbol} not found for disclosure, skipping", disc.StockSymbol);
                    continue;
                }

                // Check for duplicate by title + date + stock
                var exists = await _context.Disclosures
                    .AnyAsync(d => d.StockId == stock.Id
                        && d.TitleAr == disc.TitleAr
                        && d.PublishedDate == disc.PublishedDate, ct);

                if (exists) continue;

                var disclosureType = disc.Type switch
                {
                    "Financial" => DisclosureType.Financial,
                    "Board" => DisclosureType.Board,
                    "AGM" => DisclosureType.AGM,
                    "Dividend" => DisclosureType.Dividend,
                    _ => DisclosureType.General
                };

                _context.Disclosures.Add(new Disclosure
                {
                    StockId = stock.Id,
                    TitleAr = disc.TitleAr,
                    ContentAr = disc.ContentAr,
                    Type = disclosureType,
                    PublishedDate = disc.PublishedDate,
                    SourceUrl = disc.SourceUrl,
                    IsProcessed = false
                });
                newCount++;
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Saved {Count} new disclosures", newCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disclosure scrape");
            throw;
        }
    }
}
