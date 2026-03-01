using AlMal.Application.Interfaces;
using AlMal.Application.Services;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Jobs;

public class OrderBookScraperJob
{
    private readonly IMarketDataProvider _provider;
    private readonly AlMalDbContext _context;
    private readonly ILogger<OrderBookScraperJob> _logger;

    public OrderBookScraperJob(
        IMarketDataProvider provider,
        AlMalDbContext context,
        ILogger<OrderBookScraperJob> logger)
    {
        _provider = provider;
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        if (!KuwaitMarketHours.IsMarketOpen())
        {
            _logger.LogDebug("Market is closed, skipping order book scrape");
            return;
        }

        _logger.LogInformation("Starting order book scrape");

        var activeStocks = await _context.Stocks
            .Where(s => s.IsActive)
            .Select(s => s.Symbol)
            .ToListAsync(ct);

        var timestamp = DateTime.UtcNow;

        foreach (var symbol in activeStocks)
        {
            try
            {
                var orderBookData = await _provider.ScrapeOrderBookAsync(symbol, ct);

                var stock = await _context.Stocks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Symbol == symbol, ct);

                if (stock == null) continue;

                // Remove old order book entries for this stock
                var oldEntries = await _context.OrderBooks
                    .Where(ob => ob.StockId == stock.Id)
                    .ToListAsync(ct);
                _context.OrderBooks.RemoveRange(oldEntries);

                // Add new entries
                foreach (var entry in orderBookData)
                {
                    _context.OrderBooks.Add(new OrderBook
                    {
                        StockId = stock.Id,
                        Level = entry.Level,
                        BidPrice = entry.BidPrice,
                        BidQuantity = entry.BidQuantity,
                        AskPrice = entry.AskPrice,
                        AskQuantity = entry.AskQuantity,
                        Timestamp = timestamp
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scrape order book for {Symbol}", symbol);
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Order book scrape completed for {Count} stocks", activeStocks.Count);
    }
}
