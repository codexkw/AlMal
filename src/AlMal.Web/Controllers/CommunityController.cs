using System.Security.Claims;
using System.Text.RegularExpressions;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public partial class CommunityController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AlMalDbContext _context;
    private const int PageSize = 20;
    private const int CommentsPerPage = 20;
    private const int RecentCommentsCount = 3;

    public CommunityController(
        UserManager<ApplicationUser> userManager,
        AlMalDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [GeneratedRegex(@"\$([A-Z]+)")]
    private static partial Regex StockMentionRegex();

    /// <summary>
    /// GET /Community — Community feed page
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string tab = "general", int page = 1)
    {
        if (page < 1) page = 1;
        if (tab != "general" && tab != "following") tab = "general";

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // "following" tab requires authentication
        if (tab == "following" && currentUserId == null)
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/Community?tab=following" });
        }

        IQueryable<Post> query = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (tab == "following" && currentUserId != null)
        {
            // Get IDs of users the current user follows
            var followingIds = _context.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId);

            query = query.Where(p => followingIds.Contains(p.UserId));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new PostCardViewModel
            {
                Id = p.Id,
                UserId = p.UserId,
                UserDisplayName = p.User.DisplayName,
                UserAvatarUrl = p.User.AvatarUrl,
                UserType = p.User.UserType,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                VideoUrl = p.VideoUrl,
                LikeCount = p.LikeCount,
                CommentCount = p.CommentCount,
                RepostCount = p.RepostCount,
                CreatedAt = p.CreatedAt,
                StockMentions = p.PostStockMentions.Select(sm => new StockMentionTag
                {
                    Symbol = sm.Stock.Symbol,
                    NameAr = sm.Stock.NameAr
                }).ToList(),
                RecentComments = p.Comments
                    .Where(c => !c.IsDeleted && c.ParentCommentId == null)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(RecentCommentsCount)
                    .Select(c => new CommentViewModel
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserDisplayName = c.User.DisplayName,
                        UserAvatarUrl = c.User.AvatarUrl,
                        UserType = c.User.UserType,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        ParentCommentId = c.ParentCommentId
                    }).ToList()
            })
            .ToListAsync();

        // Check which posts current user has liked
        if (currentUserId != null && posts.Count > 0)
        {
            var postIds = posts.Select(p => p.Id).ToList();
            var likedPostIds = await _context.PostLikes
                .AsNoTracking()
                .Where(pl => pl.UserId == currentUserId && postIds.Contains(pl.PostId))
                .Select(pl => pl.PostId)
                .ToListAsync();

            foreach (var post in posts)
            {
                post.IsLikedByCurrentUser = likedPostIds.Contains(post.Id);
            }
        }

        var viewModel = new CommunityFeedViewModel
        {
            Posts = posts,
            ActiveTab = tab,
            Page = page,
            TotalPages = totalPages
        };

        // Return partial for HTMX pagination requests
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_PostList", viewModel);
        }

        return View(viewModel);
    }

    /// <summary>
    /// POST /Community/Create — Create a new post
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreatePostRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        if (string.IsNullOrWhiteSpace(request.Content))
            return Json(new { success = false, error = "محتوى المنشور مطلوب." });

        if (request.Content.Length > 1000)
            return Json(new { success = false, error = "محتوى المنشور يجب ألا يتجاوز 1000 حرف." });

        var post = new Post
        {
            UserId = currentUserId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Parse $SYMBOL tags from content
        var matches = StockMentionRegex().Matches(request.Content);
        if (matches.Count > 0)
        {
            var symbols = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
            var stocks = await _context.Stocks
                .AsNoTracking()
                .Where(s => symbols.Contains(s.Symbol) && s.IsActive)
                .Select(s => new { s.Id, s.Symbol })
                .ToListAsync();

            foreach (var stock in stocks)
            {
                _context.PostStockMentions.Add(new PostStockMention
                {
                    PostId = post.Id,
                    StockId = stock.Id
                });
            }

            if (stocks.Count > 0)
                await _context.SaveChangesAsync();
        }

        // Increment user's PostCount
        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user != null)
        {
            user.PostCount++;
            await _userManager.UpdateAsync(user);
        }

        return Json(new { success = true, postId = post.Id });
    }

    /// <summary>
    /// POST /Community/Like — Toggle like on a post
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like([FromForm] long postId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        if (post == null)
            return Json(new { success = false, error = "المنشور غير موجود." });

        var existingLike = await _context.PostLikes
            .FirstOrDefaultAsync(pl => pl.UserId == currentUserId && pl.PostId == postId);

        bool liked;
        if (existingLike != null)
        {
            // Unlike: remove like and decrement count
            _context.PostLikes.Remove(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
            liked = false;
        }
        else
        {
            // Like: add like and increment count
            _context.PostLikes.Add(new PostLike
            {
                UserId = currentUserId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            });
            post.LikeCount++;
            liked = true;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, liked, likeCount = post.LikeCount });
    }

    /// <summary>
    /// POST /Community/Comment — Add a comment to a post
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comment([FromForm] long postId, [FromForm] string content, [FromForm] long? parentCommentId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        if (string.IsNullOrWhiteSpace(content))
            return Json(new { success = false, error = "محتوى التعليق مطلوب." });

        if (content.Length > 500)
            return Json(new { success = false, error = "التعليق يجب ألا يتجاوز 500 حرف." });

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        if (post == null)
            return Json(new { success = false, error = "المنشور غير موجود." });

        // Validate parent comment if provided
        if (parentCommentId.HasValue)
        {
            var parentExists = await _context.Comments
                .AsNoTracking()
                .AnyAsync(c => c.Id == parentCommentId.Value && c.PostId == postId && !c.IsDeleted);

            if (!parentExists)
                return Json(new { success = false, error = "التعليق الأصلي غير موجود." });
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = currentUserId,
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        post.CommentCount++;
        await _context.SaveChangesAsync();

        // Load user info for the partial view
        var user = await _userManager.FindByIdAsync(currentUserId);

        var commentVm = new CommentViewModel
        {
            Id = comment.Id,
            UserId = currentUserId,
            UserDisplayName = user?.DisplayName ?? "",
            UserAvatarUrl = user?.AvatarUrl,
            UserType = user?.UserType ?? Domain.Enums.UserType.Normal,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            ParentCommentId = comment.ParentCommentId
        };

        return PartialView("_Comment", commentVm);
    }

    /// <summary>
    /// POST /Community/Delete — Soft delete a post (owner only)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] long postId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        if (post == null)
            return Json(new { success = false, error = "المنشور غير موجود." });

        // Only the post owner can delete
        if (post.UserId != currentUserId)
            return Json(new { success = false, error = "لا يمكنك حذف هذا المنشور." });

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        // Decrement user's PostCount
        var user = await _userManager.FindByIdAsync(currentUserId);
        if (user != null)
        {
            user.PostCount = Math.Max(0, user.PostCount - 1);
            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// POST /Community/Report — Flag a post for moderation
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report([FromForm] long postId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);
        if (post == null)
            return Json(new { success = false, error = "المنشور غير موجود." });

        post.IsFlagged = true;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    /// <summary>
    /// GET /Community/Comments/{postId} — Paginated comments for a post (HTMX)
    /// </summary>
    [HttpGet("Community/Comments/{postId:long}")]
    public async Task<IActionResult> PostComments(long postId, int page = 1)
    {
        if (page < 1) page = 1;

        var postExists = await _context.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Id == postId && !p.IsDeleted);

        if (!postExists)
            return NotFound();

        var totalCount = await _context.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null)
            .CountAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)CommentsPerPage);

        var comments = await _context.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * CommentsPerPage)
            .Take(CommentsPerPage)
            .Select(c => new CommentViewModel
            {
                Id = c.Id,
                UserId = c.UserId,
                UserDisplayName = c.User.DisplayName,
                UserAvatarUrl = c.User.AvatarUrl,
                UserType = c.User.UserType,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                ParentCommentId = c.ParentCommentId,
                Replies = c.Replies
                    .Where(r => !r.IsDeleted)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new CommentViewModel
                    {
                        Id = r.Id,
                        UserId = r.UserId,
                        UserDisplayName = r.User.DisplayName,
                        UserAvatarUrl = r.User.AvatarUrl,
                        UserType = r.User.UserType,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        ParentCommentId = r.ParentCommentId
                    }).ToList()
            })
            .ToListAsync();

        ViewBag.PostId = postId;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;

        return PartialView("_CommentList", comments);
    }
}
