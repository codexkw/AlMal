using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersApiController : ControllerBase
{
    private readonly AlMalDbContext _db;

    public UsersApiController(AlMalDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/users/{id} — User profile
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserProfileDto
            {
                UserId = u.Id,
                DisplayName = ((ApplicationUser)u).DisplayName,
                Bio = ((ApplicationUser)u).Bio,
                AvatarUrl = ((ApplicationUser)u).AvatarUrl,
                UserType = ((ApplicationUser)u).UserType.ToString(),
                IsVerified = ((ApplicationUser)u).IsVerified,
                FollowersCount = ((ApplicationUser)u).FollowersCount,
                FollowingCount = ((ApplicationUser)u).FollowingCount,
                PostCount = ((ApplicationUser)u).PostCount,
                IsFollowing = currentUserId != null
                    && _db.UserFollows.Any(f => f.FollowerId == currentUserId && f.FollowingId == u.Id)
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return NotFound(ApiResponse<UserProfileDto>.Fail("USER_NOT_FOUND", "المستخدم غير موجود"));

        return Ok(ApiResponse<UserProfileDto>.Ok(user));
    }

    /// <summary>
    /// GET /api/v1/users/{id}/posts — User's posts (paginated)
    /// </summary>
    [HttpGet("{id}/posts")]
    public async Task<IActionResult> GetUserPosts(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == id);

        if (!userExists)
            return NotFound(ApiResponse<List<PostDto>>.Fail("USER_NOT_FOUND", "المستخدم غير موجود"));

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.PostStockMentions)
                .ThenInclude(m => m.Stock)
            .Where(p => p.UserId == id && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync();

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserDisplayName = p.User.DisplayName,
                UserAvatarUrl = p.User.AvatarUrl,
                UserType = p.User.UserType.ToString(),
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                VideoUrl = p.VideoUrl,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                RepostCount = p.RepostCount,
                IsLikedByCurrentUser = currentUserId != null
                    && p.PostLikes.Any(l => l.UserId == currentUserId),
                CreatedAt = p.CreatedAt,
                StockMentions = p.PostStockMentions
                    .Select(m => m.Stock.Symbol)
                    .ToList()
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<PostDto>>.Ok(posts, pagination));
    }

    /// <summary>
    /// POST /api/v1/users/{id}/follow — Follow a user
    /// </summary>
    [Authorize]
    [HttpPost("{id}/follow")]
    public async Task<IActionResult> Follow(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        if (currentUserId == id)
            return BadRequest(ApiResponse<object>.Fail("SELF_FOLLOW", "لا يمكنك متابعة نفسك"));

        var targetUser = await _db.Users.FindAsync(id);
        if (targetUser is null)
            return NotFound(ApiResponse<object>.Fail("USER_NOT_FOUND", "المستخدم غير موجود"));

        var alreadyFollowing = await _db.UserFollows
            .AnyAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);

        if (alreadyFollowing)
            return BadRequest(ApiResponse<object>.Fail("ALREADY_FOLLOWING", "أنت تتابع هذا المستخدم بالفعل"));

        _db.UserFollows.Add(new UserFollow
        {
            FollowerId = currentUserId,
            FollowingId = id
        });

        // Update counters
        var currentUser = await _db.Users.FindAsync(currentUserId);
        if (currentUser is ApplicationUser currentAppUser)
        {
            currentAppUser.FollowingCount++;
        }
        if (targetUser is ApplicationUser targetAppUser)
        {
            targetAppUser.FollowersCount++;
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "تمت المتابعة بنجاح" }));
    }

    /// <summary>
    /// DELETE /api/v1/users/{id}/follow — Unfollow a user
    /// </summary>
    [Authorize]
    [HttpDelete("{id}/follow")]
    public async Task<IActionResult> Unfollow(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var follow = await _db.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FollowingId == id);

        if (follow is null)
            return BadRequest(ApiResponse<object>.Fail("NOT_FOLLOWING", "أنت لا تتابع هذا المستخدم"));

        _db.UserFollows.Remove(follow);

        // Update counters
        var currentUser = await _db.Users.FindAsync(currentUserId);
        if (currentUser is ApplicationUser currentAppUser)
        {
            currentAppUser.FollowingCount = Math.Max(0, currentAppUser.FollowingCount - 1);
        }
        var targetUser = await _db.Users.FindAsync(id);
        if (targetUser is ApplicationUser targetAppUser)
        {
            targetAppUser.FollowersCount = Math.Max(0, targetAppUser.FollowersCount - 1);
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "تم إلغاء المتابعة بنجاح" }));
    }

    /// <summary>
    /// GET /api/v1/users/{id}/followers — Followers list (paginated)
    /// </summary>
    [HttpGet("{id}/followers")]
    public async Task<IActionResult> GetFollowers(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == id);

        if (!userExists)
            return NotFound(ApiResponse<List<UserProfileDto>>.Fail("USER_NOT_FOUND", "المستخدم غير موجود"));

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowingId == id)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync();

        var followers = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new UserProfileDto
            {
                UserId = f.Follower.Id,
                DisplayName = f.Follower.DisplayName,
                Bio = f.Follower.Bio,
                AvatarUrl = f.Follower.AvatarUrl,
                UserType = f.Follower.UserType.ToString(),
                IsVerified = f.Follower.IsVerified,
                FollowersCount = f.Follower.FollowersCount,
                FollowingCount = f.Follower.FollowingCount,
                PostCount = f.Follower.PostCount,
                IsFollowing = currentUserId != null
                    && _db.UserFollows.Any(uf => uf.FollowerId == currentUserId && uf.FollowingId == f.FollowerId)
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<UserProfileDto>>.Ok(followers, pagination));
    }

    /// <summary>
    /// GET /api/v1/users/{id}/following — Following list (paginated)
    /// </summary>
    [HttpGet("{id}/following")]
    public async Task<IActionResult> GetFollowing(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var userExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == id);

        if (!userExists)
            return NotFound(ApiResponse<List<UserProfileDto>>.Fail("USER_NOT_FOUND", "المستخدم غير موجود"));

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _db.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == id)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync();

        var following = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new UserProfileDto
            {
                UserId = f.FollowingUser.Id,
                DisplayName = f.FollowingUser.DisplayName,
                Bio = f.FollowingUser.Bio,
                AvatarUrl = f.FollowingUser.AvatarUrl,
                UserType = f.FollowingUser.UserType.ToString(),
                IsVerified = f.FollowingUser.IsVerified,
                FollowersCount = f.FollowingUser.FollowersCount,
                FollowingCount = f.FollowingUser.FollowingCount,
                PostCount = f.FollowingUser.PostCount,
                IsFollowing = currentUserId != null
                    && _db.UserFollows.Any(uf => uf.FollowerId == currentUserId && uf.FollowingId == f.FollowingId)
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<UserProfileDto>>.Ok(following, pagination));
    }
}
