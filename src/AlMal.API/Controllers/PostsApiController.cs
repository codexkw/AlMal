using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/posts")]
public class PostsApiController : ControllerBase
{
    private readonly AlMalDbContext _db;

    public PostsApiController(AlMalDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/posts — Paginated feed (general or following)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] string tab = "general",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.PostStockMentions)
                .ThenInclude(m => m.Stock)
            .Where(p => !p.IsDeleted);

        // If tab is "following" and user is authenticated, filter to followed users' posts
        if (tab == "following" && currentUserId != null)
        {
            var followingIds = await _db.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId)
                .ToListAsync();

            query = query.Where(p => followingIds.Contains(p.UserId));
        }

        query = query.OrderByDescending(p => p.CreatedAt);

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
    /// GET /api/v1/posts/{id} — Single post with comments
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetPost(long id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var post = await _db.Posts
            .AsNoTracking()
            .Include(p => p.User)
            .Include(p => p.PostStockMentions)
                .ThenInclude(m => m.Stock)
            .Where(p => p.Id == id && !p.IsDeleted)
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
            .FirstOrDefaultAsync();

        if (post is null)
        {
            return NotFound(ApiResponse<PostDto>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));
        }

        return Ok(ApiResponse<PostDto>.Ok(post));
    }

    /// <summary>
    /// POST /api/v1/posts — Create a new post
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<PostDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(ApiResponse<PostDto>.Fail("VALIDATION_ERROR", "محتوى المنشور مطلوب"));

        var post = new Domain.Entities.Post
        {
            UserId = userId,
            Content = dto.Content.Trim()
        };

        _db.Posts.Add(post);

        // Increment user's PostCount
        var user = await _db.Users.FindAsync(userId);
        if (user is not null)
        {
            var appUser = (Domain.Entities.ApplicationUser)user;
            appUser.PostCount++;
        }

        await _db.SaveChangesAsync();

        var result = new PostDto
        {
            Id = post.Id,
            UserId = post.UserId,
            UserDisplayName = user is Domain.Entities.ApplicationUser au ? au.DisplayName : "",
            UserAvatarUrl = user is Domain.Entities.ApplicationUser au2 ? au2.AvatarUrl : null,
            UserType = user is Domain.Entities.ApplicationUser au3 ? au3.UserType.ToString() : "Normal",
            Content = post.Content,
            LikeCount = 0,
            CommentCount = 0,
            RepostCount = 0,
            IsLikedByCurrentUser = false,
            CreatedAt = post.CreatedAt,
            StockMentions = []
        };

        return Ok(ApiResponse<PostDto>.Ok(result));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/like — Toggle like on a post
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/like")]
    public async Task<IActionResult> ToggleLike(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var post = await _db.Posts.FindAsync(id);
        if (post is null || post.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        var existingLike = await _db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == userId);

        bool isLiked;

        if (existingLike is not null)
        {
            // Unlike
            _db.PostLikes.Remove(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
            isLiked = false;
        }
        else
        {
            // Like
            _db.PostLikes.Add(new Domain.Entities.PostLike
            {
                UserId = userId,
                PostId = id
            });
            post.LikeCount++;
            isLiked = true;
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            isLiked,
            likeCount = post.LikeCount
        }));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/comments — Add a comment to a post
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/comments")]
    public async Task<IActionResult> AddComment(long id, [FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<CommentDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(ApiResponse<CommentDto>.Fail("VALIDATION_ERROR", "محتوى التعليق مطلوب"));

        var post = await _db.Posts.FindAsync(id);
        if (post is null || post.IsDeleted)
            return NotFound(ApiResponse<CommentDto>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        // Validate parent comment if specified
        if (dto.ParentCommentId.HasValue)
        {
            var parentExists = await _db.Comments
                .AsNoTracking()
                .AnyAsync(c => c.Id == dto.ParentCommentId.Value && c.PostId == id && !c.IsDeleted);

            if (!parentExists)
                return BadRequest(ApiResponse<CommentDto>.Fail("PARENT_NOT_FOUND", "التعليق الأصلي غير موجود"));
        }

        var comment = new Domain.Entities.Comment
        {
            PostId = id,
            UserId = userId,
            Content = dto.Content.Trim(),
            ParentCommentId = dto.ParentCommentId
        };

        _db.Comments.Add(comment);
        post.CommentCount++;
        await _db.SaveChangesAsync();

        // Load user info for response
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { ((Domain.Entities.ApplicationUser)u).DisplayName, ((Domain.Entities.ApplicationUser)u).AvatarUrl, ((Domain.Entities.ApplicationUser)u).UserType })
            .FirstOrDefaultAsync();

        var result = new CommentDto
        {
            Id = comment.Id,
            UserId = comment.UserId,
            UserDisplayName = user?.DisplayName ?? "",
            UserAvatarUrl = user?.AvatarUrl,
            UserType = user?.UserType.ToString() ?? "Normal",
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ParentCommentId = comment.ParentCommentId,
            Replies = []
        };

        return Ok(ApiResponse<CommentDto>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/posts/{id}/comments — Paginated comments for a post
    /// </summary>
    [HttpGet("{id:long}/comments")]
    public async Task<IActionResult> GetComments(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var postExists = await _db.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Id == id && !p.IsDeleted);

        if (!postExists)
            return NotFound(ApiResponse<List<CommentDto>>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        // Get top-level comments only (no parent)
        var query = _db.Comments
            .AsNoTracking()
            .Include(c => c.User)
            .Include(c => c.Replies)
                .ThenInclude(r => r.User)
            .Where(c => c.PostId == id && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();

        var comments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserDisplayName = c.User.DisplayName,
                UserAvatarUrl = c.User.AvatarUrl,
                UserType = c.User.UserType.ToString(),
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                ParentCommentId = c.ParentCommentId,
                Replies = c.Replies
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new CommentDto
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserDisplayName = r.User.DisplayName,
                        UserAvatarUrl = r.User.AvatarUrl,
                        UserType = r.User.UserType.ToString(),
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        ParentCommentId = r.ParentCommentId,
                        Replies = new List<CommentDto>()
                    })
                    .ToList()
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<CommentDto>>.Ok(comments, pagination));
    }

    /// <summary>
    /// DELETE /api/v1/posts/{id} — Soft delete a post (owner only)
    /// </summary>
    [Authorize]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeletePost(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var post = await _db.Posts.FindAsync(id);
        if (post is null || post.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        if (post.UserId != userId)
            return StatusCode(403, ApiResponse<object>.Fail("FORBIDDEN", "لا يمكنك حذف منشور مستخدم آخر"));

        post.IsDeleted = true;

        // Decrement user's PostCount
        var user = await _db.Users.FindAsync(userId);
        if (user is Domain.Entities.ApplicationUser appUser)
        {
            appUser.PostCount = Math.Max(0, appUser.PostCount - 1);
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "تم حذف المنشور بنجاح" }));
    }

    /// <summary>
    /// POST /api/v1/posts/{id}/report — Report a post (flags it for moderation)
    /// </summary>
    [Authorize]
    [HttpPost("{id:long}/report")]
    public async Task<IActionResult> ReportPost(long id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var post = await _db.Posts.FindAsync(id);
        if (post is null || post.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("POST_NOT_FOUND", "المنشور غير موجود"));

        // Flag the post for moderation review
        post.IsFlagged = true;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "تم الإبلاغ عن المنشور بنجاح" }));
    }
}
