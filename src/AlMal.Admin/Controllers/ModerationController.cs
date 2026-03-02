using AlMal.Admin.ViewModels;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ModerationController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<ModerationController> _logger;
    private const int PageSize = 20;

    public ModerationController(AlMalDbContext context, ILogger<ModerationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Moderation queue — flagged posts
    /// </summary>
    public async Task<IActionResult> Index(int page = 1)
    {
        if (page < 1) page = 1;

        var query = _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.IsFlagged && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new ModerationItemViewModel
            {
                Id = p.Id,
                Type = "منشور",
                Content = p.Content,
                UserDisplayName = p.User.DisplayName,
                UserId = p.UserId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var viewModel = new ModerationQueueViewModel
        {
            Items = items,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST — Approve (unflag) a post
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(long postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post is null)
        {
            TempData["Error"] = "المنشور غير موجود";
            return RedirectToAction(nameof(Index));
        }

        post.IsFlagged = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} approved (unflagged) by admin {Admin}", postId, User.Identity?.Name);
        TempData["Success"] = "تمت الموافقة على المنشور بنجاح";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// POST — Soft delete a flagged post
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        if (post is null)
        {
            TempData["Error"] = "المنشور غير موجود";
            return RedirectToAction(nameof(Index));
        }

        post.IsDeleted = true;
        post.IsFlagged = false;

        // Decrement user's PostCount
        var user = await _context.Users.FindAsync(post.UserId);
        if (user is ApplicationUser appUser)
        {
            appUser.PostCount = Math.Max(0, appUser.PostCount - 1);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Post {PostId} deleted by admin {Admin}", postId, User.Identity?.Name);
        TempData["Success"] = "تم حذف المنشور بنجاح";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// User management list
    /// </summary>
    public async Task<IActionResult> Users(string? search, string? type, int page = 1)
    {
        if (page < 1) page = 1;

        var query = _context.Users
            .AsNoTracking()
            .OfType<ApplicationUser>()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                u.DisplayName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<UserType>(type, out var userType))
        {
            query = query.Where(u => u.UserType == userType);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page > totalPages && totalPages > 0) page = totalPages;

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(u => new UserManagementItemViewModel
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                Email = u.Email,
                UserType = u.UserType,
                IsActive = u.IsActive,
                PostCount = u.PostCount,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        var viewModel = new UserManagementViewModel
        {
            Users = users,
            SearchTerm = search,
            TypeFilter = type,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST — Suspend a user (IsActive = false)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SuspendUser(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is not ApplicationUser appUser)
        {
            TempData["Error"] = "المستخدم غير موجود";
            return RedirectToAction(nameof(Users));
        }

        appUser.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} suspended by admin {Admin}", userId, User.Identity?.Name);
        TempData["Success"] = "تم إيقاف المستخدم بنجاح";

        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// POST — Activate a user (IsActive = true)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is not ApplicationUser appUser)
        {
            TempData["Error"] = "المستخدم غير موجود";
            return RedirectToAction(nameof(Users));
        }

        appUser.IsActive = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} activated by admin {Admin}", userId, User.Identity?.Name);
        TempData["Success"] = "تم تفعيل المستخدم بنجاح";

        return RedirectToAction(nameof(Users));
    }

    /// <summary>
    /// POST — Promote user (change UserType) — SuperAdmin only
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> PromoteUser(string userId, UserType type)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is not ApplicationUser appUser)
        {
            TempData["Error"] = "المستخدم غير موجود";
            return RedirectToAction(nameof(Users));
        }

        appUser.UserType = type;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} promoted to {UserType} by admin {Admin}", userId, type, User.Identity?.Name);
        TempData["Success"] = $"تم تغيير نوع المستخدم إلى {GetUserTypeArabic(type)} بنجاح";

        return RedirectToAction(nameof(Users));
    }

    private static string GetUserTypeArabic(UserType type) => type switch
    {
        UserType.Normal => "عادي",
        UserType.ProAnalyst => "محلل محترف",
        UserType.CertifiedAnalyst => "محلل معتمد",
        _ => type.ToString()
    };
}
