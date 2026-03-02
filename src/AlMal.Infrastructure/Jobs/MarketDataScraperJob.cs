using AlMal.Application.DTOs.Market;
using AlMal.Application.Interfaces;
using AlMal.Application.Services;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

public class MarketDataScraperJob
{
    private readonly IMarketDataProvider _provider;
    private readonly AlMalDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MarketDataScraperJob> _logger;

    public MarketDataScraperJob(
        IMarketDataProvider provider,
        AlMalDbContext context,
        IDistributedCache cache,
        ILogger<MarketDataScraperJob> logger)
    {
        _provider = provider;
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!KuwaitMarketHours.IsMarketOpen())
        {
            _logger.LogDebug("Market is closed, skipping stock price scrape");
            return;
        }

        _logger.LogInformation("Starting market data scrape at {Time}", KuwaitMarketHours.GetKuwaitTime());

        try
        {
            // Scrape stock prices
            var stockPrices = await _provider.ScrapeStockPricesAsync(ct);
            _logger.LogInformation("Scraped {Count} stock prices", stockPrices.Count);

            foreach (var priceData in stockPrices)
            {
                var stock = await _context.Stocks
                    .FirstOrDefaultAsync(s => s.Symbol == priceData.Symbol, ct);

                if (stock == null)
                {
                    _logger.LogWarning("Stock {Symbol} not found in database, skipping", priceData.Symbol);
                    continue;
                }

                // Update cached price fields on Stock
                stock.LastPrice = priceData.LastPrice;
                stock.DayChange = priceData.DayChange;
                stock.DayChangePercent = priceData.DayChangePercent;
            }

            // Scrape market indices
            var indices = await _provider.ScrapeMarketIndicesAsync(ct);
            _logger.LogInformation("Scraped {Count} market indices", indices.Count);

            foreach (var indexData in indices)
            {
                var marketIndexType = indexData.Type switch
                {
                    "Premier" => MarketIndexType.Premier,
                    "Sector" => MarketIndexType.Sector,
                    _ => MarketIndexType.Main
                };

                var existingIndex = await _context.MarketIndices
                    .FirstOrDefaultAsync(i => i.NameAr == indexData.NameAr, ct);

                if (existingIndex != null)
                {
                    existingIndex.Value = indexData.Value;
                    existingIndex.Change = indexData.Change;
                    existingIndex.ChangePercent = indexData.ChangePercent;
                    existingIndex.Date = DateTime.UtcNow;
                }
                else
                {
                    _context.MarketIndices.Add(new MarketIndex
                    {
                        NameAr = indexData.NameAr,
                        Type = marketIndexType,
                        Value = indexData.Value,
                        Change = indexData.Change,
                        ChangePercent = indexData.ChangePercent,
                        Date = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(ct);

            // Invalidate cache
            await _cache.RemoveAsync("market:indices", ct);
            await _cache.RemoveAsync("market:gainers", ct);
            await _cache.RemoveAsync("market:losers", ct);
            await _cache.RemoveAsync("market:heatmap", ct);

            _logger.LogInformation("Market data scrape completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market data scrape");
            throw;
        }
    }
}
