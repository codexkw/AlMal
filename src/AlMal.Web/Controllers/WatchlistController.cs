using System.Security.Claims;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Watchlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public record CreateAlertRequest(AlertType Type, int? StockId, string Condition, decimal? TargetValue, AlertChannel Channel);

[Authorize]
public class WatchlistController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WatchlistController(AlMalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// GET /Watchlist — shows user's watchlisted stocks with prices from DB
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var watchlistItems = await _context.Watchlists
            .AsNoTracking()
            .Include(w => w.Stock)
                .ThenInclude(s => s.Sector)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WatchlistItemViewModel
            {
                WatchlistId = w.Id,
                StockId = w.StockId,
                Symbol = w.Stock.Symbol,
                NameAr = w.Stock.NameAr,
                SectorNameAr = w.Stock.Sector.NameAr,
                LastPrice = w.Stock.LastPrice,
                DayChange = w.Stock.DayChange,
                DayChangePercent = w.Stock.DayChangePercent,
                AlertPrice = w.AlertPrice,
                AlertEnabled = w.AlertEnabled
            })
            .ToListAsync();

        var viewModel = new WatchlistPageViewModel
        {
            Items = watchlistItems
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST /Watchlist/Toggle — adds or removes stock from watchlist
    /// Returns JSON { success: true, added: true/false }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Toggle(int stockId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(new { success = false, error = "غير مصرح" });

        var existing = await _context.Watchlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.StockId == stockId);

        if (existing != null)
        {
            _context.Watchlists.Remove(existing);
            await _context.SaveChangesAsync();
            return Json(new { success = true, added = false });
        }

        // Verify stock exists
        var stockExists = await _context.Stocks.AnyAsync(s => s.Id == stockId && s.IsActive);
        if (!stockExists)
            return NotFound(new { success = false, error = "السهم غير موجود" });

        var watchlistItem = new Domain.Entities.Watchlist
        {
            UserId = userId,
            StockId = stockId,
            AlertEnabled = false
        };

        _context.Watchlists.Add(watchlistItem);
        await _context.SaveChangesAsync();

        return Json(new { success = true, added = true });
    }

    /// <summary>
    /// GET /Watchlist/Alerts — shows all user alerts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Alerts()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var alerts = await _context.Alerts
            .AsNoTracking()
            .Include(a => a.Stock)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AlertItemViewModel
            {
                AlertId = a.Id,
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

        var viewModel = new AlertListViewModel
        {
            Alerts = alerts
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST /Watchlist/CreateAlert — creates new alert, returns JSON
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAlert([FromBody] CreateAlertRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(new { success = false, error = "غير مصرح" });

        if (string.IsNullOrWhiteSpace(request.Condition))
            return BadRequest(new { success = false, error = "يجب تحديد شرط التنبيه" });

        // Verify stock exists if StockId is provided
        if (request.StockId.HasValue)
        {
            var stockExists = await _context.Stocks.AnyAsync(s => s.Id == request.StockId.Value && s.IsActive);
            if (!stockExists)
                return NotFound(new { success = false, error = "السهم غير موجود" });
        }

        var alert = new Alert
        {
            UserId = userId,
            Type = request.Type,
            StockId = request.StockId,
            Condition = request.Condition,
            TargetValue = request.TargetValue,
            Channel = request.Channel,
            IsActive = true
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        return Json(new { success = true, alertId = alert.Id });
    }

    /// <summary>
    /// POST /Watchlist/ToggleAlert — enables/disables alert
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleAlert(int alertId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(new { success = false, error = "غير مصرح" });

        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

        if (alert == null)
            return NotFound(new { success = false, error = "التنبيه غير موجود" });

        alert.IsActive = !alert.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = alert.IsActive });
    }

    /// <summary>
    /// POST /Watchlist/DeleteAlert — deletes alert
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteAlert(int alertId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized(new { success = false, error = "غير مصرح" });

        var alert = await _context.Alerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

        if (alert == null)
            return NotFound(new { success = false, error = "التنبيه غير موجود" });

        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }
}
