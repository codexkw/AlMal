using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Market;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class MarketController : Controller
{
    private readonly AlMalDbContext _context;
    private const int PageSize = 20;

    public MarketController(AlMalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? sortBy,
        string? sortDir,
        int? sector,
        int page = 1)
    {
        // Indices
        var indices = await _context.MarketIndices
            .AsNoTracking()
            .OrderBy(i => i.Type)
            .Select(i => new MarketIndexViewModel
            {
                Id = i.Id,
                NameAr = i.NameAr,
                Type = i.Type.ToString(),
                Value = i.Value,
                Change = i.Change,
                ChangePercent = i.ChangePercent
            })
            .ToListAsync();

        // Top Gainers
        var topGainers = await GetTopStocksAsync(
            q => q.Where(s => s.DayChangePercent > 0)
                  .OrderByDescending(s => s.DayChangePercent), 5);

        // Top Losers
        var topLosers = await GetTopStocksAsync(
            q => q.Where(s => s.DayChangePercent < 0)
                  .OrderBy(s => s.DayChangePercent), 5);

        // Most Traded (by last known volume — using StockPrices for today)
        var mostTraded = await GetTopStocksAsync(
            q => q.Where(s => s.LastPrice > 0)
                  .OrderByDescending(s => s.MarketCap), 5);

        // All Stocks query
        var stocksQuery = _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive);

        // Search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            stocksQuery = stocksQuery.Where(s =>
                s.Symbol.Contains(search) ||
                s.NameAr.Contains(search) ||
                (s.NameEn != null && s.NameEn.Contains(search)));
        }

        // Sector filter
        if (sector.HasValue)
        {
            stocksQuery = stocksQuery.Where(s => s.SectorId == sector.Value);
        }

        // Sorting
        stocksQuery = (sortBy?.ToLower(), sortDir?.ToLower()) switch
        {
            ("symbol", "asc") => stocksQuery.OrderBy(s => s.Symbol),
            ("symbol", _) => stocksQuery.OrderByDescending(s => s.Symbol),
            ("price", "asc") => stocksQuery.OrderBy(s => s.LastPrice),
            ("price", _) => stocksQuery.OrderByDescending(s => s.LastPrice),
            ("change", "asc") => stocksQuery.OrderBy(s => s.DayChangePercent),
            ("change", _) => stocksQuery.OrderByDescending(s => s.DayChangePercent),
            ("name", "asc") => stocksQuery.OrderBy(s => s.NameAr),
            ("name", _) => stocksQuery.OrderByDescending(s => s.NameAr),
            _ => stocksQuery.OrderBy(s => s.Symbol)
        };

        var totalStocks = await stocksQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalStocks / (double)PageSize);

        var allStocks = await stocksQuery
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(s => new StockViewModel
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap
            })
            .ToListAsync();

        // Sectors
        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorViewModel
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                IndexValue = s.IndexValue,
                ChangePercent = s.ChangePercent,
                StockCount = s.Stocks.Count(st => st.IsActive)
            })
            .ToListAsync();

        var viewModel = new MarketDashboardViewModel
        {
            Indices = indices,
            TopGainers = topGainers,
            TopLosers = topLosers,
            MostTraded = mostTraded,
            AllStocks = allStocks,
            Sectors = sectors,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalStocks = totalStocks,
            SearchQuery = search,
            SortBy = sortBy,
            SortDirection = sortDir,
            SectorFilter = sector
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q)
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
                s.Symbol,
                s.NameAr,
                s.LastPrice,
                s.DayChangePercent
            })
            .ToListAsync();

        return Json(results);
    }

    private async Task<List<StockViewModel>> GetTopStocksAsync(
        Func<IQueryable<AlMal.Domain.Entities.Stock>, IQueryable<AlMal.Domain.Entities.Stock>> filter,
        int count)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive);

        return await filter(query)
            .Take(count)
            .Select(s => new StockViewModel
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap
            })
            .ToListAsync();
    }
}
