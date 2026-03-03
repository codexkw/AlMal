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
                CreatedAt = p.CreatedAt,
                ImageUrl = p.ImageUrl,
                VideoUrl = p.VideoUrl,
                ReportReason = p.ReportReason,
                ReportedByUserId = p.ReportedByUserId,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                UserType = p.User.UserType
            })
            .ToListAsync();

        // Load reporter display names
        var reporterIds = items
            .Where(i => !string.IsNullOrEmpty(i.ReportedByUserId))
            .Select(i => i.ReportedByUserId!)
            .Distinct()
            .ToList();

        if (reporterIds.Count > 0)
        {
            var reporters = await _context.Users
                .AsNoTracking()
                .OfType<ApplicationUser>()
                .Where(u => reporterIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

            foreach (var item in items)
            {
                if (item.ReportedByUserId != null && reporters.TryGetValue(item.ReportedByUserId, out var name))
                    item.ReportedByDisplayName = name;
            }
        }

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
    /// GET — Review detail page for a flagged post
    /// </summary>
    public async Task<IActionResult> Review(long id)
    {
        var post = await _context.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.Comments.Where(c => !c.IsDeleted).OrderByDescending(c => c.CreatedAt).Take(10))
                .ThenInclude(c => c.User)
            .Include(p => p.PostStockMentions)
                .ThenInclude(m => m.Stock)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();

        if (post == null)
        {
            TempData["Error"] = "المنشور غير موجود";
            return RedirectToAction(nameof(Index));
        }

        string? reporterName = null;
        if (!string.IsNullOrEmpty(post.ReportedByUserId))
        {
            reporterName = await _context.Users
                .AsNoTracking()
                .OfType<ApplicationUser>()
                .Where(u => u.Id == post.ReportedByUserId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();
        }

        var item = new ModerationItemViewModel
        {
            Id = post.Id,
            Type = "منشور",
            Content = post.Content,
            UserDisplayName = post.User.DisplayName,
            UserId = post.UserId,
            CreatedAt = post.CreatedAt,
            ImageUrl = post.ImageUrl,
            VideoUrl = post.VideoUrl,
            ReportReason = post.ReportReason,
            ReportedByUserId = post.ReportedByUserId,
            ReportedByDisplayName = reporterName,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            UserType = post.User.UserType
        };

        ViewBag.Comments = post.Comments.Select(c => new
        {
            c.Id,
            c.Content,
            c.CreatedAt,
            UserDisplayName = c.User.DisplayName
        }).ToList();

        ViewBag.StockMentions = post.PostStockMentions.Select(m => new
        {
            m.Stock.Symbol,
            m.Stock.NameAr
        }).ToList();

        return View(item);
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
        post.ReportReason = null;
        post.ReportedByUserId = null;
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

    /// <summary>
    /// Badge approval queue
    /// </summary>
    public async Task<IActionResult> BadgeRequests(int page = 1)
    {
        if (page < 1) page = 1;

        var query = _context.Set<BadgeRequest>()
            .AsNoTracking()
            .Include(br => br.User)
            .Where(br => br.Status == BadgeRequestStatus.Pending)
            .OrderByDescending(br => br.RequestedAt);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page > totalPages && totalPages > 0) page = totalPages;

        var requests = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(br => new BadgeRequestItemViewModel
            {
                Id = br.Id,
                UserId = br.UserId,
                DisplayName = br.User.DisplayName,
                Email = br.User.Email,
                CurrentType = br.User.UserType,
                RequestedType = br.RequestedType,
                Justification = br.Justification,
                CertificateUrl = br.CertificateUrl,
                RequestedAt = br.RequestedAt,
                PostCount = br.User.PostCount,
                FollowersCount = br.User.FollowersCount
            })
            .ToListAsync();

        var viewModel = new BadgeRequestListViewModel
        {
            Requests = requests,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST — Approve a badge request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ApproveBadge(long requestId)
    {
        var request = await _context.Set<BadgeRequest>()
            .Include(br => br.User)
            .FirstOrDefaultAsync(br => br.Id == requestId);

        if (request == null)
        {
            TempData["Error"] = "طلب الشارة غير موجود";
            return RedirectToAction(nameof(BadgeRequests));
        }

        request.Status = BadgeRequestStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Upgrade user type
        request.User.UserType = request.RequestedType;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Badge request {RequestId} approved for user {UserId} to {Type} by admin {Admin}",
            requestId, request.UserId, request.RequestedType, User.Identity?.Name);
        TempData["Success"] = $"تمت الموافقة على طلب شارة {GetUserTypeArabic(request.RequestedType)} بنجاح";

        return RedirectToAction(nameof(BadgeRequests));
    }

    /// <summary>
    /// POST — Reject a badge request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RejectBadge(long requestId, string? rejectionReason)
    {
        var request = await _context.Set<BadgeRequest>()
            .FirstOrDefaultAsync(br => br.Id == requestId);

        if (request == null)
        {
            TempData["Error"] = "طلب الشارة غير موجود";
            return RedirectToAction(nameof(BadgeRequests));
        }

        request.Status = BadgeRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        request.RejectionReason = rejectionReason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Badge request {RequestId} rejected for user {UserId} by admin {Admin}: {Reason}",
            requestId, request.UserId, User.Identity?.Name, rejectionReason ?? "(no reason)");
        TempData["Success"] = "تم رفض طلب الشارة";

        return RedirectToAction(nameof(BadgeRequests));
    }

    private static string GetUserTypeArabic(UserType type) => type switch
    {
        UserType.Normal => "عادي",
        UserType.ProAnalyst => "محلل محترف",
        UserType.CertifiedAnalyst => "محلل معتمد",
        _ => type.ToString()
    };
}
