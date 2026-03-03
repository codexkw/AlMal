using System.Security.Claims;
using AlMal.Application.Interfaces;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Portfolio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

[Authorize]
public class PortfolioController : Controller
{
    private readonly ISimulationService _simulation;
    private readonly AlMalDbContext _context;

    public PortfolioController(ISimulationService simulation, AlMalDbContext context)
    {
        _simulation = simulation;
        _context = context;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio — Dashboard
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var summary = await _simulation.GetPortfolioSummaryAsync(userId, ct);
        var sectorAllocations = await _simulation.GetSectorAllocationsAsync(summary.PortfolioId, ct);
        var performanceHistory = await _simulation.GetPerformanceHistoryAsync(summary.PortfolioId, ct);

        var viewModel = MapToViewModel(summary, sectorAllocations, performanceHistory);
        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio/Trade — Trade Form
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Trade(string? type, int? stockId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var portfolio = await _simulation.GetOrCreatePortfolioAsync(userId, ct);
        var tradeType = type?.ToLower() == "sell" ? "Sell" : "Buy";

        var viewModel = new TradeViewModel
        {
            PortfolioId = portfolio.Id,
            CashBalance = portfolio.CashBalance,
            TradeType = tradeType,
            CurrentHoldings = portfolio.Holdings
                .Where(h => h.Quantity > 0)
                .Select(h => new HoldingViewModel
                {
                    StockId = h.StockId,
                    Symbol = h.Stock.Symbol,
                    NameAr = h.Stock.NameAr,
                    Quantity = h.Quantity,
                    AverageCost = h.AverageCost,
                    CurrentPrice = h.Stock.LastPrice ?? 0
                })
                .ToList()
        };

        if (stockId.HasValue)
        {
            var stock = await _context.Stocks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stockId.Value && s.IsActive, ct);

            if (stock != null)
            {
                viewModel.StockId = stock.Id;
                viewModel.StockSymbol = stock.Symbol;
                viewModel.StockNameAr = stock.NameAr;
                viewModel.StockPrice = stock.LastPrice;

                if (tradeType == "Sell")
                {
                    var holding = portfolio.Holdings.FirstOrDefault(h => h.StockId == stock.Id);
                    viewModel.MaxQuantity = holding?.Quantity ?? 0;
                }
            }
        }

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // POST /Portfolio/ExecuteTrade — Execute Buy/Sell
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteTrade(string tradeType, int stockId, int quantity, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var type = tradeType?.ToLower() == "sell" ? TradeType.Sell : TradeType.Buy;
        var result = await _simulation.ExecuteTradeAsync(userId, stockId, quantity, type, ct);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Trade", new { type = tradeType, stockId });
        }

        var stock = await _context.Stocks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId, ct);

        TempData["Success"] = type == TradeType.Buy
            ? $"تم شراء {quantity} سهم من {stock?.Symbol ?? ""} بنجاح"
            : $"تم بيع {quantity} سهم من {stock?.Symbol ?? ""} بنجاح";

        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════════
    // POST /Portfolio/Reset — Reset Portfolio
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        await _simulation.ResetPortfolioAsync(userId, ct);
        TempData["Success"] = "تم إعادة تعيين المحفظة بنجاح";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════════
    // POST /Portfolio/TogglePublic — Toggle Portfolio Visibility
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublic(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        await _simulation.TogglePublicAsync(userId, ct);

        var portfolio = await _simulation.GetOrCreatePortfolioAsync(userId, ct);
        TempData["Success"] = portfolio.IsPublic
            ? "المحفظة الآن عامة ويمكن للآخرين رؤيتها"
            : "المحفظة الآن خاصة";

        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio/Public/{userId} — View Public Portfolio
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Public(string userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return NotFound();

        var summary = await _simulation.GetPublicPortfolioAsync(userId, ct);
        if (summary == null)
            return NotFound();

        var sectorAllocations = await _simulation.GetSectorAllocationsAsync(summary.PortfolioId, ct);
        var performanceHistory = await _simulation.GetPerformanceHistoryAsync(summary.PortfolioId, ct);

        var viewModel = MapToViewModel(summary, sectorAllocations, performanceHistory);

        ViewData["OwnerName"] = summary.OwnerDisplayName;
        ViewData["IsPublicView"] = true;

        return View("Index", viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio/SearchStock — Stock search autocomplete (JSON)
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SearchStock(string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
            return Json(new List<object>());

        var results = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive && (
                s.Symbol.Contains(q) ||
                s.NameAr.Contains(q) ||
                (s.NameEn != null && s.NameEn.Contains(q))))
            .Take(10)
            .Select(s => new
            {
                id = s.Id,
                symbol = s.Symbol,
                nameAr = s.NameAr,
                lastPrice = s.LastPrice
            })
            .ToListAsync(ct);

        return Json(results);
    }

    // ── Private Helpers ──────────────────────────────────────────

    private static PortfolioDashboardViewModel MapToViewModel(
        PortfolioSummary summary,
        List<SectorAllocation> sectorAllocations,
        List<PerformancePoint> performanceHistory)
    {
        return new PortfolioDashboardViewModel
        {
            PortfolioId = summary.PortfolioId,
            InitialCapital = summary.InitialCapital,
            CashBalance = summary.CashBalance,
            TotalPortfolioValue = summary.TotalPortfolioValue,
            PnL = summary.PnL,
            PnLPercent = summary.PnLPercent,
            IsPublic = summary.IsPublic,
            Holdings = summary.Holdings.Select(h => new HoldingViewModel
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
            RecentTrades = summary.RecentTrades.Select(t => new TradeHistoryViewModel
            {
                Id = t.Id,
                Symbol = t.Symbol,
                NameAr = t.NameAr,
                Type = t.Type,
                Quantity = t.Quantity,
                Price = t.Price,
                TotalValue = t.TotalValue,
                ExecutedAt = t.ExecutedAt
            }).ToList(),
            SectorAllocations = sectorAllocations.Select(s => new SectorAllocationViewModel
            {
                SectorNameAr = s.SectorNameAr,
                MarketValue = s.MarketValue,
                WeightPercent = s.WeightPercent
            }).ToList(),
            PerformanceHistory = performanceHistory.Select(p => new PerformancePointViewModel
            {
                Date = p.Date.ToString("yyyy-MM-dd"),
                Value = p.PortfolioValue
            }).ToList()
        };
    }
}
