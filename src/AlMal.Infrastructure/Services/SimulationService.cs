using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Services;

public class SimulationService : ISimulationService
{
    private readonly AlMalDbContext _db;
    private readonly ILogger<SimulationService> _logger;
    private const decimal DefaultInitialCapital = 100_000m;

    public SimulationService(AlMalDbContext db, ILogger<SimulationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SimulationPortfolio> GetOrCreatePortfolioAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
                .ThenInclude(h => h.Stock)
                    .ThenInclude(s => s.Sector)
            .Include(p => p.Trades)
                .ThenInclude(t => t.Stock)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (portfolio == null)
        {
            portfolio = new SimulationPortfolio
            {
                UserId = userId,
                InitialCapital = DefaultInitialCapital,
                CashBalance = DefaultInitialCapital,
                IsPublic = false
            };
            _db.SimulationPortfolios.Add(portfolio);
            await _db.SaveChangesAsync(ct);

            portfolio = await _db.SimulationPortfolios
                .Include(p => p.Holdings)
                    .ThenInclude(h => h.Stock)
                        .ThenInclude(s => s.Sector)
                .Include(p => p.Trades)
                    .ThenInclude(t => t.Stock)
                .FirstAsync(p => p.UserId == userId, ct);
        }

        return portfolio;
    }

    public async Task<PortfolioSummary> GetPortfolioSummaryAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await GetOrCreatePortfolioAsync(userId, ct);
        return BuildSummary(portfolio);
    }

    public async Task<TradeResult> ExecuteTradeAsync(string userId, int stockId, int quantity, TradeType type, CancellationToken ct = default)
    {
        if (quantity <= 0)
            return TradeResult.Fail("VALIDATION_ERROR", "الكمية يجب أن تكون أكبر من صفر");

        var stock = await _db.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId && s.IsActive, ct);

        if (stock == null)
            return TradeResult.Fail("STOCK_NOT_FOUND", "السهم غير موجود");

        var price = stock.LastPrice ?? 0;
        if (price <= 0)
            return TradeResult.Fail("NO_PRICE", "لا يتوفر سعر للسهم حالياً");

        var portfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (portfolio == null)
        {
            portfolio = (await GetOrCreatePortfolioAsync(userId, ct));
        }

        var totalValue = quantity * price;

        if (type == TradeType.Buy)
        {
            if (totalValue > portfolio.CashBalance)
                return TradeResult.Fail("INSUFFICIENT_BALANCE",
                    $"الرصيد النقدي غير كافٍ. المتاح: {portfolio.CashBalance:N3} د.ك");

            portfolio.CashBalance -= totalValue;

            var holding = portfolio.Holdings.FirstOrDefault(h => h.StockId == stockId);
            if (holding != null)
            {
                var totalCost = (holding.Quantity * holding.AverageCost) + totalValue;
                var totalQty = holding.Quantity + quantity;
                holding.AverageCost = totalQty > 0 ? totalCost / totalQty : 0;
                holding.Quantity = totalQty;
            }
            else
            {
                _db.SimulationHoldings.Add(new SimulationHolding
                {
                    PortfolioId = portfolio.Id,
                    StockId = stockId,
                    Quantity = quantity,
                    AverageCost = price
                });
            }
        }
        else
        {
            var holding = portfolio.Holdings.FirstOrDefault(h => h.StockId == stockId);
            if (holding == null || holding.Quantity < quantity)
                return TradeResult.Fail("INSUFFICIENT_HOLDINGS", "لا تملك كمية كافية من هذا السهم");

            portfolio.CashBalance += totalValue;
            holding.Quantity -= quantity;

            if (holding.Quantity == 0)
                _db.SimulationHoldings.Remove(holding);
        }

        _db.SimulationTrades.Add(new SimulationTrade
        {
            PortfolioId = portfolio.Id,
            StockId = stockId,
            Type = type,
            Quantity = quantity,
            Price = price,
            TotalValue = totalValue,
            ExecutedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Simulation trade executed: {Type} {Quantity} shares of StockId={StockId} at {Price} for user {UserId}",
            type, quantity, stockId, price, userId);

        return TradeResult.Ok();
    }

    public async Task ResetPortfolioAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
            .Include(p => p.Trades)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (portfolio == null)
            return;

        _db.SimulationHoldings.RemoveRange(portfolio.Holdings);
        _db.SimulationTrades.RemoveRange(portfolio.Trades);
        portfolio.CashBalance = portfolio.InitialCapital;

        await _db.SaveChangesAsync(ct);
    }

    public async Task TogglePublicAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await _db.SimulationPortfolios
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (portfolio == null)
            return;

        portfolio.IsPublic = !portfolio.IsPublic;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PortfolioSummary?> GetPublicPortfolioAsync(string userId, CancellationToken ct = default)
    {
        var portfolio = await _db.SimulationPortfolios
            .AsNoTracking()
            .Include(p => p.Holdings)
                .ThenInclude(h => h.Stock)
                    .ThenInclude(s => s.Sector)
            .Include(p => p.Trades)
                .ThenInclude(t => t.Stock)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsPublic, ct);

        if (portfolio == null)
            return null;

        var summary = BuildSummary(portfolio);
        summary.OwnerDisplayName = portfolio.User.DisplayName;
        return summary;
    }

    public async Task<List<SectorAllocation>> GetSectorAllocationsAsync(int portfolioId, CancellationToken ct = default)
    {
        var holdings = await _db.SimulationHoldings
            .AsNoTracking()
            .Where(h => h.PortfolioId == portfolioId && h.Quantity > 0)
            .Include(h => h.Stock)
                .ThenInclude(s => s.Sector)
            .ToListAsync(ct);

        if (holdings.Count == 0)
            return new List<SectorAllocation>();

        var totalValue = holdings.Sum(h => h.Quantity * (h.Stock.LastPrice ?? 0));
        if (totalValue <= 0)
            return new List<SectorAllocation>();

        return holdings
            .GroupBy(h => new { h.Stock.SectorId, h.Stock.Sector.NameAr })
            .Select(g => new SectorAllocation
            {
                SectorId = g.Key.SectorId,
                SectorNameAr = g.Key.NameAr,
                MarketValue = g.Sum(h => h.Quantity * (h.Stock.LastPrice ?? 0)),
                WeightPercent = totalValue > 0
                    ? g.Sum(h => h.Quantity * (h.Stock.LastPrice ?? 0)) / totalValue * 100
                    : 0
            })
            .OrderByDescending(s => s.WeightPercent)
            .ToList();
    }

    public async Task<List<PerformancePoint>> GetPerformanceHistoryAsync(int portfolioId, CancellationToken ct = default)
    {
        // Build performance history from trade records
        // Each trade changes the portfolio state; we compute cumulative value at each trade point
        var portfolio = await _db.SimulationPortfolios
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == portfolioId, ct);

        if (portfolio == null)
            return new List<PerformancePoint>();

        var trades = await _db.SimulationTrades
            .AsNoTracking()
            .Where(t => t.PortfolioId == portfolioId)
            .OrderBy(t => t.ExecutedAt)
            .Select(t => new { t.Type, t.TotalValue, t.ExecutedAt })
            .ToListAsync(ct);

        if (trades.Count == 0)
        {
            return new List<PerformancePoint>
            {
                new() { Date = portfolio.CreatedAt, PortfolioValue = portfolio.InitialCapital }
            };
        }

        var points = new List<PerformancePoint>
        {
            new() { Date = portfolio.CreatedAt, PortfolioValue = portfolio.InitialCapital }
        };

        // Group trades by day and compute cumulative portfolio value (cash + cost basis)
        var dailyTrades = trades
            .GroupBy(t => t.ExecutedAt.Date)
            .OrderBy(g => g.Key);

        var cashBalance = portfolio.InitialCapital;
        decimal totalInvested = 0;

        foreach (var day in dailyTrades)
        {
            foreach (var trade in day)
            {
                if (trade.Type == TradeType.Buy)
                {
                    cashBalance -= trade.TotalValue;
                    totalInvested += trade.TotalValue;
                }
                else
                {
                    cashBalance += trade.TotalValue;
                    totalInvested -= trade.TotalValue;
                }
            }

            // Portfolio value approximation = cash + invested (at cost)
            // This shows capital deployment over time
            points.Add(new PerformancePoint
            {
                Date = day.Key,
                PortfolioValue = cashBalance + Math.Max(totalInvested, 0)
            });
        }

        // Add current value as the last point
        var currentHoldings = await _db.SimulationHoldings
            .AsNoTracking()
            .Where(h => h.PortfolioId == portfolioId && h.Quantity > 0)
            .Include(h => h.Stock)
            .ToListAsync(ct);

        var currentHoldingsValue = currentHoldings.Sum(h => h.Quantity * (h.Stock.LastPrice ?? 0));

        // Replace or add today's point with actual current value
        var currentCash = await _db.SimulationPortfolios
            .AsNoTracking()
            .Where(p => p.Id == portfolioId)
            .Select(p => p.CashBalance)
            .FirstAsync(ct);

        var todayPoint = points.FirstOrDefault(p => p.Date.Date == DateTime.UtcNow.Date);
        if (todayPoint != null)
        {
            todayPoint.PortfolioValue = currentCash + currentHoldingsValue;
        }
        else
        {
            points.Add(new PerformancePoint
            {
                Date = DateTime.UtcNow,
                PortfolioValue = currentCash + currentHoldingsValue
            });
        }

        return points.OrderBy(p => p.Date).ToList();
    }

    private static PortfolioSummary BuildSummary(SimulationPortfolio portfolio)
    {
        var holdings = new List<HoldingSummary>();
        decimal holdingsMarketValue = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var currentPrice = holding.Stock.LastPrice ?? 0;
            var marketValue = holding.Quantity * currentPrice;
            var costBasis = holding.Quantity * holding.AverageCost;
            var pnl = marketValue - costBasis;
            var pnlPercent = costBasis > 0 ? (pnl / costBasis) * 100 : 0;

            holdingsMarketValue += marketValue;

            holdings.Add(new HoldingSummary
            {
                StockId = holding.StockId,
                Symbol = holding.Stock.Symbol,
                NameAr = holding.Stock.NameAr,
                SectorNameAr = holding.Stock.Sector?.NameAr,
                SectorId = holding.Stock.SectorId,
                Quantity = holding.Quantity,
                AverageCost = holding.AverageCost,
                CurrentPrice = currentPrice,
                MarketValue = marketValue,
                PnL = pnl,
                PnLPercent = pnlPercent
            });
        }

        var totalPortfolioValue = portfolio.CashBalance + holdingsMarketValue;
        foreach (var h in holdings)
        {
            h.WeightPercent = totalPortfolioValue > 0
                ? (h.MarketValue / totalPortfolioValue) * 100
                : 0;
        }

        var pnlTotal = totalPortfolioValue - portfolio.InitialCapital;
        var pnlTotalPercent = portfolio.InitialCapital > 0
            ? (pnlTotal / portfolio.InitialCapital) * 100
            : 0;

        var recentTrades = portfolio.Trades
            .OrderByDescending(t => t.ExecutedAt)
            .Take(20)
            .Select(t => new TradeSummary
            {
                Id = t.Id,
                Symbol = t.Stock.Symbol,
                NameAr = t.Stock.NameAr,
                Type = t.Type,
                Quantity = t.Quantity,
                Price = t.Price,
                TotalValue = t.TotalValue,
                ExecutedAt = t.ExecutedAt
            })
            .ToList();

        return new PortfolioSummary
        {
            PortfolioId = portfolio.Id,
            InitialCapital = portfolio.InitialCapital,
            CashBalance = portfolio.CashBalance,
            TotalPortfolioValue = totalPortfolioValue,
            PnL = pnlTotal,
            PnLPercent = pnlTotalPercent,
            IsPublic = portfolio.IsPublic,
            Holdings = holdings,
            RecentTrades = recentTrades
        };
    }
}
