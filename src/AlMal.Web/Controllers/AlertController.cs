using System.Security.Claims;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Alert;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

[Authorize]
public class AlertController : Controller
{
    private readonly AlMalDbContext _context;

    public AlertController(AlMalDbContext context)
    {
        _context = context;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// GET /Alerts — List user's alerts with status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var alerts = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.Stock)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertItemViewModel
            {
                Id = a.Id,
                Type = a.Type,
                StockSymbol = a.Stock != null ? a.Stock.Symbol : null,
                StockNameAr = a.Stock != null ? a.Stock.NameAr : null,
                Condition = a.Condition,
                TargetValue = a.TargetValue,
                Channel = a.Channel,
                IsActive = a.IsActive,
                LastTriggered = a.LastTriggered,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var model = new AlertListViewModel { Alerts = alerts };
        return View(model);
    }

    /// <summary>
    /// GET /Alerts/Create — Create alert form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var stocks = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.NameAr)
            .Select(s => new StockOptionViewModel
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr
            })
            .ToListAsync();

        var model = new CreateAlertViewModel
        {
            AvailableStocks = stocks
        };

        return View(model);
    }

    /// <summary>
    /// POST /Alerts/Create — Create new alert.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAlertViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableStocks = await GetStockOptionsAsync();
            return View(model);
        }

        // Validate stock is required for price/volume/disclosure alerts
        if (model.Type is AlertType.Price or AlertType.Volume or AlertType.Disclosure)
        {
            if (!model.StockId.HasValue)
            {
                ModelState.AddModelError(nameof(model.StockId), "يرجى اختيار السهم");
                model.AvailableStocks = await GetStockOptionsAsync();
                return View(model);
            }
        }

        // Validate target value for price alerts
        if (model.Type == AlertType.Price && !model.TargetValue.HasValue)
        {
            ModelState.AddModelError(nameof(model.TargetValue), "يرجى تحديد السعر المستهدف");
            model.AvailableStocks = await GetStockOptionsAsync();
            return View(model);
        }

        var userId = GetUserId();

        var alert = new Alert
        {
            UserId = userId,
            Type = model.Type,
            StockId = model.StockId,
            Condition = model.Condition,
            TargetValue = model.TargetValue,
            Channel = model.Channel,
            IsActive = true
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم إنشاء التنبيه بنجاح";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST /Alerts/Toggle/{id} — Enable/disable alert.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = GetUserId();
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (alert == null)
            return NotFound();

        alert.IsActive = !alert.IsActive;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST /Alerts/Delete/{id} — Delete alert.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (alert == null)
            return NotFound();

        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "تم حذف التنبيه";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<StockOptionViewModel>> GetStockOptionsAsync()
    {
        return await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.NameAr)
            .Select(s => new StockOptionViewModel
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr
            })
            .ToListAsync();
    }
}
