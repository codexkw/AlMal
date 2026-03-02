using System.Security.Claims;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly AlMalDbContext _context;

    public NotificationController(AlMalDbContext context)
    {
        _context = context;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>
    /// GET /Notifications — List all notifications for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                Title = n.Title,
                Body = n.Body,
                Type = n.Type,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        var unreadCount = notifications.Count(n => !n.IsRead);

        var model = new NotificationListViewModel
        {
            Notifications = notifications,
            UnreadCount = unreadCount
        };

        return View(model);
    }

    /// <summary>
    /// POST /Notifications/MarkRead/{id} — Mark a single notification as read.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id)
    {
        var userId = GetUserId();
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST /Notifications/MarkAllRead — Mark all notifications as read.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET /Notifications/Count — Returns unread count as JSON (for notification bell).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var userId = GetUserId();
        var unreadCount = await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Json(new { count = unreadCount });
    }
}
