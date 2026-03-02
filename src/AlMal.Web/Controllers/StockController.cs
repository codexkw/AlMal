using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class StockController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly IAiAnalysisService _aiService;
    private readonly ILogger<StockController> _logger;

    public StockController(AlMalDbContext context, IAiAnalysisService aiService, ILogger<StockController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpGet("stock/{symbol}")]
    public async Task<IActionResult> Detail(string symbol)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .FirstOrDefaultAsync(s => s.Symbol == symbol && s.IsActive);

        if (stock == null)
            return NotFound();

        // Latest financial statement
        var latestFinancial = await _context.FinancialStatements
            .AsNoTracking()
            .Where(f => f.StockId == stock.Id)
            .OrderByDescending(f => f.Year)
            .ThenByDescending(f => f.Quarter)
            .FirstOrDefaultAsync();

        FinancialRatiosViewModel? financials = null;
        if (latestFinancial != null)
        {
            financials = new FinancialRatiosViewModel
            {
                EPS = latestFinancial.EPS,
                DPS = latestFinancial.DPS,
                Year = latestFinancial.Year,
                Quarter = latestFinancial.Quarter,
                PE = latestFinancial.EPS > 0 && stock.LastPrice.HasValue
                    ? stock.LastPrice.Value / latestFinancial.EPS.GetValueOrDefault(1)
                    : null,
                PB = latestFinancial.BookValue > 0 && stock.LastPrice.HasValue
                    ? stock.LastPrice.Value / latestFinancial.BookValue.GetValueOrDefault(1)
                    : null,
                ROE = latestFinancial.TotalEquity > 0
                    ? (latestFinancial.NetIncome / latestFinancial.TotalEquity.GetValueOrDefault(1)) * 100
                    : null,
                ProfitMargin = latestFinancial.Revenue > 0
                    ? (latestFinancial.NetIncome / latestFinancial.Revenue.GetValueOrDefault(1)) * 100
                    : null,
                DividendYield = latestFinancial.DPS > 0 && stock.LastPrice.HasValue && stock.LastPrice.Value > 0
                    ? (latestFinancial.DPS.GetValueOrDefault() / stock.LastPrice.Value) * 100
                    : null,
                DebtEquity = latestFinancial.TotalEquity > 0
                    ? latestFinancial.TotalDebt / latestFinancial.TotalEquity.GetValueOrDefault(1)
                    : null
            };
        }

        // Recent disclosures (last 10)
        var disclosures = await _context.Disclosures
            .AsNoTracking()
            .Where(d => d.StockId == stock.Id)
            .OrderByDescending(d => d.PublishedDate)
            .Take(10)
            .Select(d => new DisclosureViewModel
            {
                Id = d.Id,
                TitleAr = d.TitleAr,
                AiSummary = d.AiSummary,
                Type = d.Type,
                PublishedDate = d.PublishedDate,
                SourceUrl = d.SourceUrl
            })
            .ToListAsync();

        // Order book
        var orderBook = await _context.OrderBooks
            .AsNoTracking()
            .Where(ob => ob.StockId == stock.Id)
            .OrderBy(ob => ob.Level)
            .Select(ob => new OrderBookEntryViewModel
            {
                Level = ob.Level,
                BidPrice = ob.BidPrice,
                BidQuantity = ob.BidQuantity,
                AskPrice = ob.AskPrice,
                AskQuantity = ob.AskQuantity
            })
            .ToListAsync();

        var totalBid = orderBook.Sum(ob => ob.BidQuantity ?? 0);
        var totalAsk = orderBook.Sum(ob => ob.AskQuantity ?? 0);
        var total = totalBid + totalAsk;

        var viewModel = new StockDetailViewModel
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            NameAr = stock.NameAr,
            NameEn = stock.NameEn,
            SectorNameAr = stock.Sector.NameAr,
            SectorId = stock.SectorId,
            LastPrice = stock.LastPrice,
            DayChange = stock.DayChange,
            DayChangePercent = stock.DayChangePercent,
            DescriptionAr = stock.DescriptionAr,
            ListingDate = stock.ListingDate,
            MarketCap = stock.MarketCap,
            SharesOutstanding = stock.SharesOutstanding,
            Financials = financials,
            RecentDisclosures = disclosures,
            OrderBook = orderBook,
            BuyPressure = total > 0 ? (decimal)totalBid / total * 100 : 50,
            SellPressure = total > 0 ? (decimal)totalAsk / total * 100 : 50
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Prices(string symbol, string? range)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Symbol == symbol);

        if (stock == null) return NotFound();

        var startDate = range switch
        {
            "1D" => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            "1W" => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            "1M" => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            "3M" => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)),
            "6M" => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            "1Y" => DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            _ => DateOnly.MinValue
        };

        var prices = await _context.StockPrices
            .AsNoTracking()
            .Where(p => p.StockId == stock.Id && p.Date >= startDate)
            .OrderBy(p => p.Date)
            .Select(p => new
            {
                time = p.Date.ToString("yyyy-MM-dd"),
                open = p.Open,
                high = p.High,
                low = p.Low,
                close = p.Close,
                volume = p.Volume
            })
            .ToListAsync();

        return Json(prices);
    }

    [HttpGet]
    public async Task<IActionResult> Disclosures(string symbol, int page = 1)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Symbol == symbol);

        if (stock == null) return NotFound();

        var disclosures = await _context.Disclosures
            .AsNoTracking()
            .Where(d => d.StockId == stock.Id)
            .OrderByDescending(d => d.PublishedDate)
            .Skip((page - 1) * 10)
            .Take(10)
            .Select(d => new DisclosureViewModel
            {
                Id = d.Id,
                TitleAr = d.TitleAr,
                AiSummary = d.AiSummary,
                Type = d.Type,
                PublishedDate = d.PublishedDate,
                SourceUrl = d.SourceUrl
            })
            .ToListAsync();

        return Json(disclosures);
    }

    [HttpGet]
    public async Task<IActionResult> ExplainMovement(string symbol, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest(new { success = false, error = "الرمز مطلوب" });

        try
        {
            var result = await _aiService.ExplainMovementAsync(symbol, ct);
            return Json(new
            {
                success = true,
                explanation = result.Explanation,
                disclaimer = result.Disclaimer
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error explaining movement for {Symbol}", symbol);
            return Json(new
            {
                success = false,
                error = "عذراً، لا يمكن تحليل حركة السهم حالياً. يرجى المحاولة لاحقاً."
            });
        }
    }
}
