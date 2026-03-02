using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/portfolio")]
[Authorize]
public class PortfolioApiController : ControllerBase
{
    private readonly AlMalDbContext _db;
    private const decimal DefaultInitialCapital = 100_000m;

    public PortfolioApiController(AlMalDbContext db)
    {
        _db = db;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<SimulationPortfolio> GetOrCreatePortfolioAsync(string userId)
    {
        var portfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
                .ThenInclude(h => h.Stock)
            .Include(p => p.Trades)
                .ThenInclude(t => t.Stock)
            .FirstOrDefaultAsync(p => p.UserId == userId);

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
            await _db.SaveChangesAsync();

            // Reload with includes
            portfolio = await _db.SimulationPortfolios
                .Include(p => p.Holdings)
                    .ThenInclude(h => h.Stock)
                .Include(p => p.Trades)
                    .ThenInclude(t => t.Stock)
                .FirstAsync(p => p.UserId == userId);
        }

        return portfolio;
    }

    private PortfolioDto BuildPortfolioDto(SimulationPortfolio portfolio)
    {
        var holdings = new List<HoldingDto>();
        decimal holdingsMarketValue = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var currentPrice = holding.Stock.LastPrice ?? 0;
            var marketValue = holding.Quantity * currentPrice;
            var costBasis = holding.Quantity * holding.AverageCost;
            var pnl = marketValue - costBasis;
            var pnlPercent = costBasis > 0 ? (pnl / costBasis) * 100 : 0;

            holdingsMarketValue += marketValue;

            holdings.Add(new HoldingDto
            {
                StockId = holding.StockId,
                Symbol = holding.Stock.Symbol,
                NameAr = holding.Stock.NameAr,
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
            .Select(t => new TradeDto
            {
                Id = t.Id,
                Symbol = t.Stock.Symbol,
                NameAr = t.Stock.NameAr,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                Price = t.Price,
                TotalValue = t.TotalValue,
                ExecutedAt = t.ExecutedAt
            })
            .ToList();

        return new PortfolioDto
        {
            Id = portfolio.Id,
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

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/portfolio — Get Portfolio
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/portfolio — Get the user's simulation portfolio
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPortfolio()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PortfolioDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var portfolio = await GetOrCreatePortfolioAsync(userId);
        var dto = BuildPortfolioDto(portfolio);

        return Ok(ApiResponse<PortfolioDto>.Ok(dto));
    }

    // ════════════════════════════════════════════════════════════
    // POST /api/v1/portfolio/trade — Execute Trade
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/v1/portfolio/trade — Execute a simulation trade (buy or sell)
    /// </summary>
    [HttpPost("trade")]
    public async Task<IActionResult> ExecuteTrade([FromBody] ExecuteTradeDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PortfolioDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        if (dto.Quantity <= 0)
            return BadRequest(ApiResponse<PortfolioDto>.Fail("VALIDATION_ERROR", "الكمية يجب أن تكون أكبر من صفر"));

        var stock = await _db.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.StockId && s.IsActive);

        if (stock == null)
            return NotFound(ApiResponse<PortfolioDto>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));

        var price = stock.LastPrice ?? 0;
        if (price <= 0)
            return BadRequest(ApiResponse<PortfolioDto>.Fail("NO_PRICE", "لا يتوفر سعر للسهم حالياً"));

        var portfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
            .Include(p => p.Trades)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (portfolio == null)
        {
            portfolio = await GetOrCreatePortfolioAsync(userId);
        }

        var totalValue = dto.Quantity * price;
        var isBuy = dto.Type?.ToLower() != "sell";

        if (isBuy)
        {
            if (totalValue > portfolio.CashBalance)
                return BadRequest(ApiResponse<PortfolioDto>.Fail("INSUFFICIENT_BALANCE",
                    $"الرصيد النقدي غير كافٍ. المتاح: {portfolio.CashBalance.ToString("N3")} د.ك"));

            portfolio.CashBalance -= totalValue;

            var holding = portfolio.Holdings.FirstOrDefault(h => h.StockId == dto.StockId);
            if (holding != null)
            {
                var totalCost = (holding.Quantity * holding.AverageCost) + totalValue;
                var totalQty = holding.Quantity + dto.Quantity;
                holding.AverageCost = totalQty > 0 ? totalCost / totalQty : 0;
                holding.Quantity = totalQty;
            }
            else
            {
                var newHolding = new SimulationHolding
                {
                    PortfolioId = portfolio.Id,
                    StockId = dto.StockId,
                    Quantity = dto.Quantity,
                    AverageCost = price
                };
                _db.SimulationHoldings.Add(newHolding);
            }

            _db.SimulationTrades.Add(new SimulationTrade
            {
                PortfolioId = portfolio.Id,
                StockId = dto.StockId,
                Type = TradeType.Buy,
                Quantity = dto.Quantity,
                Price = price,
                TotalValue = totalValue,
                ExecutedAt = DateTime.UtcNow
            });
        }
        else
        {
            var holding = portfolio.Holdings.FirstOrDefault(h => h.StockId == dto.StockId);
            if (holding == null || holding.Quantity < dto.Quantity)
                return BadRequest(ApiResponse<PortfolioDto>.Fail("INSUFFICIENT_HOLDINGS",
                    "لا تملك كمية كافية من هذا السهم"));

            portfolio.CashBalance += totalValue;
            holding.Quantity -= dto.Quantity;

            if (holding.Quantity == 0)
            {
                _db.SimulationHoldings.Remove(holding);
            }

            _db.SimulationTrades.Add(new SimulationTrade
            {
                PortfolioId = portfolio.Id,
                StockId = dto.StockId,
                Type = TradeType.Sell,
                Quantity = dto.Quantity,
                Price = price,
                TotalValue = totalValue,
                ExecutedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        // Reload portfolio with all navigation properties for response
        var updatedPortfolio = await _db.SimulationPortfolios
            .Include(p => p.Holdings)
                .ThenInclude(h => h.Stock)
            .Include(p => p.Trades)
                .ThenInclude(t => t.Stock)
            .FirstAsync(p => p.UserId == userId);

        var resultDto = BuildPortfolioDto(updatedPortfolio);

        return Ok(ApiResponse<PortfolioDto>.Ok(resultDto));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/portfolio/performance — Performance Data
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/portfolio/performance — Get portfolio performance metrics
    /// </summary>
    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PerformanceDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var portfolio = await GetOrCreatePortfolioAsync(userId);

        // Calculate holdings value
        decimal holdingsValue = 0;
        int currentHoldingsCount = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var currentPrice = holding.Stock.LastPrice ?? 0;
            holdingsValue += holding.Quantity * currentPrice;
            currentHoldingsCount++;
        }

        var totalPortfolioValue = portfolio.CashBalance + holdingsValue;
        var pnl = totalPortfolioValue - portfolio.InitialCapital;
        var pnlPercent = portfolio.InitialCapital > 0
            ? (pnl / portfolio.InitialCapital) * 100
            : 0;

        var totalTrades = portfolio.Trades.Count;
        var buyTrades = portfolio.Trades.Count(t => t.Type == TradeType.Buy);
        var sellTrades = portfolio.Trades.Count(t => t.Type == TradeType.Sell);
        var uniqueStocks = portfolio.Trades
            .Select(t => t.StockId)
            .Distinct()
            .Count();

        var performanceDto = new PerformanceDto
        {
            InitialCapital = portfolio.InitialCapital,
            CashBalance = portfolio.CashBalance,
            HoldingsValue = holdingsValue,
            TotalPortfolioValue = totalPortfolioValue,
            PnL = pnl,
            PnLPercent = pnlPercent,
            TotalTrades = totalTrades,
            BuyTrades = buyTrades,
            SellTrades = sellTrades,
            UniqueStocksTraded = uniqueStocks,
            CurrentHoldingsCount = currentHoldingsCount
        };

        return Ok(ApiResponse<PerformanceDto>.Ok(performanceDto));
    }
}
