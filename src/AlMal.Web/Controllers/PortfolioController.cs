using System.Security.Claims;
using AlMal.Domain.Entities;
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
    private readonly AlMalDbContext _context;
    private const decimal DefaultInitialCapital = 100_000m;

    public PortfolioController(AlMalDbContext context)
    {
        _context = context;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<SimulationPortfolio> GetOrCreatePortfolioAsync(string userId)
    {
        var portfolio = await _context.SimulationPortfolios
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
            _context.SimulationPortfolios.Add(portfolio);
            await _context.SaveChangesAsync();
        }

        return portfolio;
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio — Dashboard
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var portfolio = await GetOrCreatePortfolioAsync(userId);

        // Calculate holdings value
        var holdings = new List<HoldingViewModel>();
        decimal holdingsMarketValue = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var currentPrice = holding.Stock.LastPrice ?? 0;
            var marketValue = holding.Quantity * currentPrice;
            var costBasis = holding.Quantity * holding.AverageCost;
            var pnl = marketValue - costBasis;
            var pnlPercent = costBasis > 0 ? (pnl / costBasis) * 100 : 0;

            holdingsMarketValue += marketValue;

            holdings.Add(new HoldingViewModel
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

        // Calculate weight percentages
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

        // Recent trades (last 20)
        var recentTrades = portfolio.Trades
            .OrderByDescending(t => t.ExecutedAt)
            .Take(20)
            .Select(t => new TradeHistoryViewModel
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

        var viewModel = new PortfolioDashboardViewModel
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

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio/Trade — Trade Form
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Trade(string? type, int? stockId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var portfolio = await GetOrCreatePortfolioAsync(userId);

        var tradeType = type?.ToLower() == "sell" ? "Sell" : "Buy";

        var viewModel = new TradeViewModel
        {
            PortfolioId = portfolio.Id,
            CashBalance = portfolio.CashBalance,
            TradeType = tradeType
        };

        // Build current holdings for sell dropdown
        viewModel.CurrentHoldings = portfolio.Holdings
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
            .ToList();

        // If stock is pre-selected
        if (stockId.HasValue)
        {
            var stock = await _context.Stocks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stockId.Value && s.IsActive);

            if (stock != null)
            {
                viewModel.StockId = stock.Id;
                viewModel.StockSymbol = stock.Symbol;
                viewModel.StockNameAr = stock.NameAr;
                viewModel.StockPrice = stock.LastPrice;

                // If selling, set max quantity
                if (tradeType == "Sell")
                {
                    var holding = portfolio.Holdings
                        .FirstOrDefault(h => h.StockId == stock.Id);
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
    public async Task<IActionResult> ExecuteTrade(string tradeType, int stockId, int quantity)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        if (quantity <= 0)
        {
            TempData["Error"] = "الكمية يجب أن تكون أكبر من صفر";
            return RedirectToAction("Trade", new { type = tradeType });
        }

        var stock = await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stockId && s.IsActive);

        if (stock == null)
        {
            TempData["Error"] = "السهم غير موجود";
            return RedirectToAction("Trade", new { type = tradeType });
        }

        var price = stock.LastPrice ?? 0;
        if (price <= 0)
        {
            TempData["Error"] = "لا يتوفر سعر للسهم حالياً";
            return RedirectToAction("Trade", new { type = tradeType });
        }

        var portfolio = await _context.SimulationPortfolios
            .Include(p => p.Holdings)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (portfolio == null)
        {
            TempData["Error"] = "المحفظة غير موجودة";
            return RedirectToAction("Index");
        }

        var totalValue = quantity * price;
        var isBuy = tradeType?.ToLower() != "sell";

        if (isBuy)
        {
            // Validate cash balance
            if (totalValue > portfolio.CashBalance)
            {
                TempData["Error"] = $"الرصيد النقدي غير كافٍ. المتاح: {portfolio.CashBalance.ToString("N3")} د.ك";
                return RedirectToAction("Trade", new { type = "buy", stockId });
            }

            // Deduct cash
            portfolio.CashBalance -= totalValue;

            // Update or create holding
            var holding = portfolio.Holdings
                .FirstOrDefault(h => h.StockId == stockId);

            if (holding != null)
            {
                // Weighted average cost
                var totalCost = (holding.Quantity * holding.AverageCost) + totalValue;
                var totalQty = holding.Quantity + quantity;
                holding.AverageCost = totalQty > 0 ? totalCost / totalQty : 0;
                holding.Quantity = totalQty;
            }
            else
            {
                var newHolding = new SimulationHolding
                {
                    PortfolioId = portfolio.Id,
                    StockId = stockId,
                    Quantity = quantity,
                    AverageCost = price
                };
                _context.SimulationHoldings.Add(newHolding);
            }

            // Create trade record
            var trade = new SimulationTrade
            {
                PortfolioId = portfolio.Id,
                StockId = stockId,
                Type = TradeType.Buy,
                Quantity = quantity,
                Price = price,
                TotalValue = totalValue,
                ExecutedAt = DateTime.UtcNow
            };
            _context.SimulationTrades.Add(trade);
        }
        else
        {
            // Sell
            var holding = portfolio.Holdings
                .FirstOrDefault(h => h.StockId == stockId);

            if (holding == null || holding.Quantity < quantity)
            {
                TempData["Error"] = "لا تملك كمية كافية من هذا السهم";
                return RedirectToAction("Trade", new { type = "sell", stockId });
            }

            // Add proceeds to cash
            portfolio.CashBalance += totalValue;

            // Update holding
            holding.Quantity -= quantity;

            // If quantity is zero, remove holding
            if (holding.Quantity == 0)
            {
                _context.SimulationHoldings.Remove(holding);
            }

            // Create trade record
            var trade = new SimulationTrade
            {
                PortfolioId = portfolio.Id,
                StockId = stockId,
                Type = TradeType.Sell,
                Quantity = quantity,
                Price = price,
                TotalValue = totalValue,
                ExecutedAt = DateTime.UtcNow
            };
            _context.SimulationTrades.Add(trade);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = isBuy
            ? $"تم شراء {quantity} سهم من {stock.Symbol} بنجاح"
            : $"تم بيع {quantity} سهم من {stock.Symbol} بنجاح";

        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════════
    // POST /Portfolio/Reset — Reset Portfolio
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var portfolio = await _context.SimulationPortfolios
            .Include(p => p.Holdings)
            .Include(p => p.Trades)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (portfolio == null)
            return RedirectToAction("Index");

        // Remove all holdings and trades
        _context.SimulationHoldings.RemoveRange(portfolio.Holdings);
        _context.SimulationTrades.RemoveRange(portfolio.Trades);

        // Reset cash balance
        portfolio.CashBalance = portfolio.InitialCapital;

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم إعادة تعيين المحفظة بنجاح";
        return RedirectToAction("Index");
    }

    // ════════════════════════════════════════════════════════════
    // POST /Portfolio/TogglePublic — Toggle Portfolio Visibility
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublic()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var portfolio = await _context.SimulationPortfolios
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (portfolio == null)
            return RedirectToAction("Index");

        portfolio.IsPublic = !portfolio.IsPublic;
        await _context.SaveChangesAsync();

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
    public async Task<IActionResult> Public(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return NotFound();

        var portfolio = await _context.SimulationPortfolios
            .AsNoTracking()
            .Include(p => p.Holdings)
                .ThenInclude(h => h.Stock)
            .Include(p => p.Trades)
                .ThenInclude(t => t.Stock)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsPublic);

        if (portfolio == null)
            return NotFound();

        // Build holdings
        var holdings = new List<HoldingViewModel>();
        decimal holdingsMarketValue = 0;

        foreach (var holding in portfolio.Holdings.Where(h => h.Quantity > 0))
        {
            var currentPrice = holding.Stock.LastPrice ?? 0;
            var marketValue = holding.Quantity * currentPrice;
            var costBasis = holding.Quantity * holding.AverageCost;
            var pnl = marketValue - costBasis;
            var pnlPercent = costBasis > 0 ? (pnl / costBasis) * 100 : 0;

            holdingsMarketValue += marketValue;

            holdings.Add(new HoldingViewModel
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
            .Select(t => new TradeHistoryViewModel
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

        var viewModel = new PortfolioDashboardViewModel
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

        ViewData["OwnerName"] = portfolio.User.DisplayName;
        ViewData["IsPublicView"] = true;

        return View("Index", viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Portfolio/SearchStock — Stock search autocomplete (JSON)
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> SearchStock(string q)
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
            .ToListAsync();

        return Json(results);
    }
}
