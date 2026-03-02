using System.Security.Claims;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AlMalDbContext _context;
    private const int PageSize = 20;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        AlMalDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    /// <summary>
    /// GET /Profile/{id} — Public profile page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null || !user.IsActive)
            return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwnProfile = currentUserId != null && currentUserId == id;

        var isFollowing = false;
        if (currentUserId != null && !isOwnProfile)
        {
            isFollowing = await _context.UserFollows
                .AsNoTracking()
                .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);
        }

        var viewModel = new ProfileViewModel
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            UserType = user.UserType,
            IsVerified = user.IsVerified,
            FollowersCount = user.FollowersCount,
            FollowingCount = user.FollowingCount,
            PostCount = user.PostCount,
            IsOwnProfile = isOwnProfile,
            IsFollowing = isFollowing,
            MemberSince = user.CreatedAt
        };

        return View(viewModel);
    }

    /// <summary>
    /// GET /Profile/Edit — Edit own profile form
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Edit()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return NotFound();

        var viewModel = new EditProfileViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST /Profile/Edit — Save profile changes
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
            return NotFound();

        // Validate display name
        if (string.IsNullOrWhiteSpace(model.DisplayName))
        {
            ModelState.AddModelError(nameof(model.DisplayName), "الاسم المعروض مطلوب.");
            return View(model);
        }

        if (model.DisplayName.Length > 50)
        {
            ModelState.AddModelError(nameof(model.DisplayName), "الاسم المعروض يجب ألا يتجاوز 50 حرفاً.");
            return View(model);
        }

        // Validate bio length
        if (model.Bio != null && model.Bio.Length > 200)
        {
            ModelState.AddModelError(nameof(model.Bio), "النبذة التعريفية يجب ألا تتجاوز 200 حرف.");
            return View(model);
        }

        user.DisplayName = model.DisplayName.Trim();
        user.Bio = model.Bio?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ التعديلات. حاول مرة أخرى.");
            return View(model);
        }

        TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح.";
        return RedirectToAction(nameof(Index), new { id = userId });
    }

    /// <summary>
    /// POST /Profile/Follow — Follow a user
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Follow([FromForm] string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Prevent self-follow
        if (currentUserId == userId)
            return Json(new { success = false, error = "لا يمكنك متابعة نفسك." });

        // Check target user exists
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null || !targetUser.IsActive)
            return Json(new { success = false, error = "المستخدم غير موجود." });

        // Check if already following
        var existingFollow = await _context.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

        if (existingFollow != null)
            return Json(new { success = true, followersCount = targetUser.FollowersCount });

        // Create follow record
        var follow = new UserFollow
        {
            FollowerId = currentUserId!,
            FollowingId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserFollows.Add(follow);

        // Update denormalized counts
        targetUser.FollowersCount++;

        var currentUser = await _userManager.FindByIdAsync(currentUserId!);
        if (currentUser != null)
            currentUser.FollowingCount++;

        await _context.SaveChangesAsync();

        return Json(new { success = true, followersCount = targetUser.FollowersCount });
    }

    /// <summary>
    /// POST /Profile/Unfollow — Unfollow a user
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Unfollow([FromForm] string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Prevent self-unfollow
        if (currentUserId == userId)
            return Json(new { success = false, error = "لا يمكنك إلغاء متابعة نفسك." });

        // Check target user exists
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
            return Json(new { success = false, error = "المستخدم غير موجود." });

        // Find existing follow record
        var existingFollow = await _context.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == userId);

        if (existingFollow == null)
            return Json(new { success = true, followersCount = targetUser.FollowersCount });

        // Remove follow record
        _context.UserFollows.Remove(existingFollow);

        // Update denormalized counts (prevent negative)
        targetUser.FollowersCount = Math.Max(0, targetUser.FollowersCount - 1);

        var currentUser = await _userManager.FindByIdAsync(currentUserId!);
        if (currentUser != null)
            currentUser.FollowingCount = Math.Max(0, currentUser.FollowingCount - 1);

        await _context.SaveChangesAsync();

        return Json(new { success = true, followersCount = targetUser.FollowersCount });
    }

    /// <summary>
    /// GET /Profile/Followers?id={id}&amp;page=1 — Followers list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Followers(string id, int page = 1)
    {
        return await GetFollowList(id, "followers", page);
    }

    /// <summary>
    /// GET /Profile/Following?id={id}&amp;page=1 — Following list
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Following(string id, int page = 1)
    {
        return await GetFollowList(id, "following", page);
    }

    /// <summary>
    /// Shared method for followers/following list
    /// </summary>
    private async Task<IActionResult> GetFollowList(string userId, string listType, int page)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
            return NotFound();

        if (page < 1) page = 1;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        IQueryable<UserFollow> query;
        if (listType == "followers")
        {
            query = _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowingId == userId);
        }
        else
        {
            query = _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == userId);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        List<FollowListItem> users;

        if (listType == "followers")
        {
            users = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(f => new FollowListItem
                {
                    UserId = f.FollowerId,
                    DisplayName = f.Follower.DisplayName,
                    AvatarUrl = f.Follower.AvatarUrl,
                    UserType = f.Follower.UserType,
                    IsFollowing = false // Will be updated below
                })
                .ToListAsync();
        }
        else
        {
            users = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(f => new FollowListItem
                {
                    UserId = f.FollowingId,
                    DisplayName = f.FollowingUser.DisplayName,
                    AvatarUrl = f.FollowingUser.AvatarUrl,
                    UserType = f.FollowingUser.UserType,
                    IsFollowing = false // Will be updated below
                })
                .ToListAsync();
        }

        // Determine which users in the list the current user follows
        if (currentUserId != null && users.Count > 0)
        {
            var userIds = users.Select(u => u.UserId).ToList();
            var followedIds = await _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId && userIds.Contains(f.FollowingId))
                .Select(f => f.FollowingId)
                .ToListAsync();

            foreach (var u in users)
            {
                u.IsFollowing = followedIds.Contains(u.UserId);
            }
        }

        var viewModel = new FollowListViewModel
        {
            UserId = userId,
            DisplayName = user.DisplayName,
            ListType = listType,
            Users = users,
            Page = page,
            TotalPages = totalPages
        };

        return View("Followers", viewModel);
    }
}
