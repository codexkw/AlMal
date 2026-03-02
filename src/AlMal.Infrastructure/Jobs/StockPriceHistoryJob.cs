using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

public class StockPriceHistoryJob
{
    private readonly IMarketDataProvider _provider;
    private readonly AlMalDbContext _context;
    private readonly ILogger<StockPriceHistoryJob> _logger;

    public StockPriceHistoryJob(
        IMarketDataProvider provider,
        AlMalDbContext context,
        ILogger<StockPriceHistoryJob> logger)
    {
        _provider = provider;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting stock price history update");

        var activeStocks = await _context.Stocks
            .Where(s => s.IsActive)
            .ToListAsync(ct);

        foreach (var stock in activeStocks)
        {
            try
            {
                var history = await _provider.ScrapeStockPriceHistoryAsync(stock.Symbol, ct);

                foreach (var priceData in history)
                {
                    var exists = await _context.StockPrices
                        .AnyAsync(sp => sp.StockId == stock.Id && sp.Date == priceData.Date, ct);

                    if (exists) continue;

                    _context.StockPrices.Add(new StockPrice
                    {
                        StockId = stock.Id,
                        Date = priceData.Date,
                        Open = priceData.Open,
                        High = priceData.High,
                        Low = priceData.Low,
                        Close = priceData.Close,
                        Volume = priceData.Volume,
                        Value = priceData.Value,
                        Trades = priceData.Trades
                    });
                }

                _logger.LogDebug("Updated price history for {Symbol}: {Count} new records", stock.Symbol, history.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update price history for {Symbol}", stock.Symbol);
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Stock price history update completed");
    }
}
