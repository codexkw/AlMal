using AlMal.Admin.ViewModels;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class SectorsController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<SectorsController> _logger;

    public SectorsController(AlMalDbContext context, ILogger<SectorsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorListItemViewModel
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                IsActive = true,
                StockCount = s.Stocks.Count
            })
            .ToListAsync();

        var viewModel = new SectorListViewModel
        {
            Sectors = sectors
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        var model = new SectorEditViewModel
        {
            IsActive = true
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectorEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var maxSortOrder = await _context.Sectors
            .AsNoTracking()
            .MaxAsync(s => (int?)s.SortOrder) ?? 0;

        var sector = new Sector
        {
            NameAr = model.NameAr,
            NameEn = model.NameEn,
            SortOrder = maxSortOrder + 1
        };

        _context.Sectors.Add(sector);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector created: {NameAr} (ID: {Id})", sector.NameAr, sector.Id);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sector = await _context.Sectors
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sector == null)
            return NotFound();

        var model = new SectorEditViewModel
        {
            Id = sector.Id,
            NameAr = sector.NameAr,
            NameEn = sector.NameEn,
            IsActive = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SectorEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sector = await _context.Sectors.FindAsync(model.Id);
        if (sector == null)
            return NotFound();

        sector.NameAr = model.NameAr;
        sector.NameEn = model.NameEn;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector updated: {NameAr} (ID: {Id})", sector.NameAr, sector.Id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sector = await _context.Sectors
            .Include(s => s.Stocks)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sector == null)
            return NotFound();

        if (sector.Stocks.Count > 0)
        {
            TempData["Error"] = "لا يمكن حذف القطاع لأنه يحتوي على أسهم مرتبطة";
            return RedirectToAction(nameof(Index));
        }

        _context.Sectors.Remove(sector);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sector deleted: {NameAr} (ID: {Id})", sector.NameAr, sector.Id);

        return RedirectToAction(nameof(Index));
    }
}
