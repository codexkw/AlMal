using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Application.Interfaces;
using AlMal.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/portfolio")]
[Authorize]
public class PortfolioApiController : ControllerBase
{
    private readonly ISimulationService _simulation;

    public PortfolioApiController(ISimulationService simulation)
    {
        _simulation = simulation;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/portfolio — Get Portfolio
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> GetPortfolio(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PortfolioDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var summary = await _simulation.GetPortfolioSummaryAsync(userId, ct);
        var dto = MapToDto(summary);

        return Ok(ApiResponse<PortfolioDto>.Ok(dto));
    }

    // ════════════════════════════════════════════════════════════
    // POST /api/v1/portfolio/trade — Execute Trade
    // ════════════════════════════════════════════════════════════

    [HttpPost("trade")]
    public async Task<IActionResult> ExecuteTrade([FromBody] ExecuteTradeDto dto, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PortfolioDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var tradeType = dto.Type?.ToLower() == "sell" ? TradeType.Sell : TradeType.Buy;
        var result = await _simulation.ExecuteTradeAsync(userId, dto.StockId, dto.Quantity, tradeType, ct);

        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "STOCK_NOT_FOUND" => NotFound(ApiResponse<PortfolioDto>.Fail(result.ErrorCode, result.ErrorMessage!)),
                _ => BadRequest(ApiResponse<PortfolioDto>.Fail(result.ErrorCode!, result.ErrorMessage!))
            };
        }

        var summary = await _simulation.GetPortfolioSummaryAsync(userId, ct);
        var resultDto = MapToDto(summary);

        return Ok(ApiResponse<PortfolioDto>.Ok(resultDto));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/portfolio/performance — Performance Data
    // ════════════════════════════════════════════════════════════

    [HttpGet("performance")]
    public async Task<IActionResult> GetPerformance(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<PerformanceDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var summary = await _simulation.GetPortfolioSummaryAsync(userId, ct);

        var portfolio = await _simulation.GetOrCreatePortfolioAsync(userId, ct);
        var trades = portfolio.Trades;

        var performanceDto = new PerformanceDto
        {
            InitialCapital = summary.InitialCapital,
            CashBalance = summary.CashBalance,
            HoldingsValue = summary.TotalPortfolioValue - summary.CashBalance,
            TotalPortfolioValue = summary.TotalPortfolioValue,
            PnL = summary.PnL,
            PnLPercent = summary.PnLPercent,
            TotalTrades = trades.Count,
            BuyTrades = trades.Count(t => t.Type == TradeType.Buy),
            SellTrades = trades.Count(t => t.Type == TradeType.Sell),
            UniqueStocksTraded = trades.Select(t => t.StockId).Distinct().Count(),
            CurrentHoldingsCount = summary.Holdings.Count
        };

        return Ok(ApiResponse<PerformanceDto>.Ok(performanceDto));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/portfolio/sectors — Sector Allocations
    // ════════════════════════════════════════════════════════════

    [HttpGet("sectors")]
    public async Task<IActionResult> GetSectorAllocations(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<List<SectorAllocationApiDto>>.Fail("UNAUTHORIZED", "غير مصرح"));

        var portfolio = await _simulation.GetOrCreatePortfolioAsync(userId, ct);
        var allocations = await _simulation.GetSectorAllocationsAsync(portfolio.Id, ct);

        var dtos = allocations.Select(a => new SectorAllocationApiDto
        {
            SectorNameAr = a.SectorNameAr,
            MarketValue = a.MarketValue,
            WeightPercent = a.WeightPercent
        }).ToList();

        return Ok(ApiResponse<List<SectorAllocationApiDto>>.Ok(dtos));
    }

    // ── Private Helpers ──────────────────────────────────────────

    private static PortfolioDto MapToDto(PortfolioSummary summary)
    {
        return new PortfolioDto
        {
            Id = summary.PortfolioId,
            InitialCapital = summary.InitialCapital,
            CashBalance = summary.CashBalance,
            TotalPortfolioValue = summary.TotalPortfolioValue,
            PnL = summary.PnL,
            PnLPercent = summary.PnLPercent,
            IsPublic = summary.IsPublic,
            Holdings = summary.Holdings.Select(h => new HoldingDto
            {
                StockId = h.StockId,
                Symbol = h.Symbol,
                NameAr = h.NameAr,
                Quantity = h.Quantity,
                AverageCost = h.AverageCost,
                CurrentPrice = h.CurrentPrice,
                MarketValue = h.MarketValue,
                PnL = h.PnL,
                PnLPercent = h.PnLPercent,
                WeightPercent = h.WeightPercent
            }).ToList(),
            RecentTrades = summary.RecentTrades.Select(t => new TradeDto
            {
                Id = t.Id,
                Symbol = t.Symbol,
                NameAr = t.NameAr,
                Type = t.Type.ToString(),
                Quantity = t.Quantity,
                Price = t.Price,
                TotalValue = t.TotalValue,
                ExecutedAt = t.ExecutedAt
            }).ToList()
        };
    }
}
