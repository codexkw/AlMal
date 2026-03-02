using AlMal.Admin.ViewModels;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class StocksController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<StocksController> _logger;
    private const int PageSize = 20;

    public StocksController(AlMalDbContext context, ILogger<StocksController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, int? sector, int page = 1)
    {
        var query = _context.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Symbol.Contains(search) ||
                s.NameAr.Contains(search) ||
                (s.NameEn != null && s.NameEn.Contains(search)));
        }

        if (sector.HasValue)
        {
            query = query.Where(s => s.SectorId == sector.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var stocks = await query
            .OrderBy(s => s.Symbol)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(s => new StockListItemViewModel
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                LastPrice = s.LastPrice,
                IsActive = s.IsActive
            })
            .ToListAsync();

        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorFilterItem
            {
                Id = s.Id,
                NameAr = s.NameAr
            })
            .ToListAsync();

        var viewModel = new StockListViewModel
        {
            Stocks = stocks,
            SearchTerm = search,
            SectorFilter = sector,
            Sectors = sectors,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StockEditViewModel
        {
            IsActive = true,
            Sectors = await GetSectorListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Sectors = await GetSectorListAsync();
            return View(model);
        }

        var stock = new Domain.Entities.Stock
        {
            Symbol = model.Symbol,
            NameAr = model.NameAr,
            NameEn = model.NameEn,
            SectorId = model.SectorId,
            IsActive = model.IsActive,
            DescriptionAr = model.DescriptionAr,
            SharesOutstanding = model.SharesOutstanding
        };

        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock created: {Symbol} (ID: {Id})", stock.Symbol, stock.Id);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var stock = await _context.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (stock == null)
            return NotFound();

        var model = new StockEditViewModel
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            NameAr = stock.NameAr,
            NameEn = stock.NameEn,
            SectorId = stock.SectorId,
            IsActive = stock.IsActive,
            DescriptionAr = stock.DescriptionAr,
            SharesOutstanding = stock.SharesOutstanding,
            Sectors = await GetSectorListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(StockEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Sectors = await GetSectorListAsync();
            return View(model);
        }

        var stock = await _context.Stocks.FindAsync(model.Id);
        if (stock == null)
            return NotFound();

        stock.Symbol = model.Symbol;
        stock.NameAr = model.NameAr;
        stock.NameEn = model.NameEn;
        stock.SectorId = model.SectorId;
        stock.IsActive = model.IsActive;
        stock.DescriptionAr = model.DescriptionAr;
        stock.SharesOutstanding = model.SharesOutstanding;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock updated: {Symbol} (ID: {Id})", stock.Symbol, stock.Id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var stock = await _context.Stocks.FindAsync(id);
        if (stock == null)
            return Json(new { success = false, message = "السهم غير موجود" });

        stock.IsActive = !stock.IsActive;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock {Symbol} toggled active: {IsActive}", stock.Symbol, stock.IsActive);

        return Json(new { success = true, isActive = stock.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var stock = await _context.Stocks.FindAsync(id);
        if (stock == null)
            return NotFound();

        stock.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock soft-deleted: {Symbol} (ID: {Id})", stock.Symbol, stock.Id);

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SectorFilterItem>> GetSectorListAsync()
    {
        return await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorFilterItem
            {
                Id = s.Id,
                NameAr = s.NameAr
            })
            .ToListAsync();
    }
}
