using System.Security.Claims;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class CommunityController : Controller
{
    private readonly IPostService _postService;
    private readonly AlMalDbContext _context;
    private const int PageSize = 20;
    private const int RecentCommentsCount = 3;

    public CommunityController(IPostService postService, AlMalDbContext context)
    {
        _postService = postService;
        _context = context;
    }

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

        // Use PostService for feed data
        var feedResult = await _postService.GetFeedAsync(currentUserId, tab, page, PageSize);

        // Build view models with recent comments (needs separate query for MVC partials)
        var postIds = feedResult.Posts.Select(p => p.Id).ToList();

        var recentComments = await _context.Comments
            .AsNoTracking()
            .Where(c => postIds.Contains(c.PostId) && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.PostId,
                Comment = new CommentViewModel
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserDisplayName = c.User.DisplayName,
                    UserAvatarUrl = c.User.AvatarUrl,
                    UserType = c.User.UserType,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ParentCommentId = c.ParentCommentId
                }
            })
            .ToListAsync();

        var commentsByPost = recentComments
            .GroupBy(c => c.PostId)
            .ToDictionary(
                g => g.Key,
                g => g.Take(RecentCommentsCount).Select(x => x.Comment).ToList());

        // Map stock mentions for display
        var stockMentionData = await _context.PostStockMentions
            .AsNoTracking()
            .Where(m => postIds.Contains(m.PostId))
            .Select(m => new { m.PostId, m.Stock.Symbol, m.Stock.NameAr })
            .ToListAsync();

        var stockMentionsByPost = stockMentionData
            .GroupBy(m => m.PostId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new StockMentionTag { Symbol = m.Symbol, NameAr = m.NameAr }).ToList());

        var posts = feedResult.Posts.Select(p => new PostCardViewModel
        {
            Id = p.Id,
            UserId = p.UserId,
            UserDisplayName = p.UserDisplayName,
            UserAvatarUrl = p.UserAvatarUrl,
            UserType = Enum.TryParse<Domain.Enums.UserType>(p.UserType, out var ut) ? ut : Domain.Enums.UserType.Normal,
            Content = p.Content,
            ImageUrl = p.ImageUrl,
            VideoUrl = p.VideoUrl,
            LikeCount = p.LikeCount,
            CommentCount = p.CommentCount,
            RepostCount = p.RepostCount,
            IsLikedByCurrentUser = p.IsLikedByCurrentUser,
            CreatedAt = p.CreatedAt,
            StockMentions = stockMentionsByPost.GetValueOrDefault(p.Id, []),
            RecentComments = commentsByPost.GetValueOrDefault(p.Id, []),
            IsRepost = p.IsRepost,
            OriginalPostId = p.OriginalPostId,
            OriginalPost = p.OriginalPost != null ? new PostCardViewModel
            {
                Id = p.OriginalPost.Id,
                UserId = p.OriginalPost.UserId,
                UserDisplayName = p.OriginalPost.UserDisplayName,
                UserAvatarUrl = p.OriginalPost.UserAvatarUrl,
                UserType = Enum.TryParse<Domain.Enums.UserType>(p.OriginalPost.UserType, out var out2) ? out2 : Domain.Enums.UserType.Normal,
                Content = p.OriginalPost.Content,
                ImageUrl = p.OriginalPost.ImageUrl,
                VideoUrl = p.OriginalPost.VideoUrl,
                LikeCount = p.OriginalPost.LikeCount,
                CommentCount = p.OriginalPost.CommentCount,
                RepostCount = p.OriginalPost.RepostCount,
                CreatedAt = p.OriginalPost.CreatedAt,
                StockMentions = p.OriginalPost.StockMentions
                    .Select(s => stockMentionData.FirstOrDefault(m => m.Symbol == s))
                    .Where(m => m != null)
                    .Select(m => new StockMentionTag { Symbol = m!.Symbol, NameAr = m.NameAr })
                    .ToList()
            } : null
        }).ToList();

        var viewModel = new CommunityFeedViewModel
        {
            Posts = posts,
            ActiveTab = tab,
            Page = page,
            TotalPages = feedResult.TotalPages
        };

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_PostList", viewModel);
        }

        return View(viewModel);
    }

    /// <summary>
    /// POST /Community/Create — Create a new post with optional media
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreatePostRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        Stream? imageStream = null;
        string? imageFileName = null;
        Stream? videoStream = null;
        string? videoFileName = null;

        if (request.Image != null && request.Image.Length > 0)
        {
            imageStream = request.Image.OpenReadStream();
            imageFileName = request.Image.FileName;
        }

        if (request.Video != null && request.Video.Length > 0)
        {
            videoStream = request.Video.OpenReadStream();
            videoFileName = request.Video.FileName;
        }

        try
        {
            var result = await _postService.CreatePostAsync(
                currentUserId, request.Content,
                imageStream, imageFileName,
                videoStream, videoFileName);

            if (!result.Success)
                return Json(new { success = false, error = result.ErrorMessage });

            return Json(new { success = true, postId = result.PostId });
        }
        finally
        {
            imageStream?.Dispose();
            videoStream?.Dispose();
        }
    }

    /// <summary>
    /// POST /Community/Repost — Repost an existing post
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Repost([FromForm] RepostRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        var result = await _postService.RepostAsync(currentUserId, request.OriginalPostId, request.Comment);

        if (!result.Success)
            return Json(new { success = false, error = result.ErrorMessage });

        return Json(new { success = true, postId = result.PostId });
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

        var result = await _postService.ToggleLikeAsync(currentUserId, postId);

        if (!result.Success)
            return Json(new { success = false, error = result.ErrorMessage });

        return Json(new { success = true, liked = result.IsLiked, likeCount = result.LikeCount });
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

        var result = await _postService.AddCommentAsync(currentUserId, postId, content, parentCommentId);

        if (!result.Success)
            return Json(new { success = false, error = result.ErrorMessage });

        var commentVm = new CommentViewModel
        {
            Id = result.Comment!.Id,
            UserId = result.Comment.UserId,
            UserDisplayName = result.Comment.UserDisplayName,
            UserAvatarUrl = result.Comment.UserAvatarUrl,
            UserType = Enum.TryParse<Domain.Enums.UserType>(result.Comment.UserType, out var ut) ? ut : Domain.Enums.UserType.Normal,
            Content = result.Comment.Content,
            CreatedAt = result.Comment.CreatedAt,
            ParentCommentId = result.Comment.ParentCommentId
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

        var result = await _postService.DeletePostAsync(currentUserId, postId);

        if (!result.Success)
            return Json(new { success = false, error = result.ErrorMessage });

        return Json(new { success = true });
    }

    /// <summary>
    /// POST /Community/Report — Flag a post for moderation
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report([FromForm] long postId, [FromForm] string? reason)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null)
            return Json(new { success = false, error = "يجب تسجيل الدخول أولاً." });

        var result = await _postService.ReportPostAsync(currentUserId, postId, reason);

        if (!result.Success)
            return Json(new { success = false, error = result.ErrorMessage });

        return Json(new { success = true });
    }

    /// <summary>
    /// GET /Community/Comments/{postId} — Paginated comments for a post (HTMX)
    /// </summary>
    [HttpGet("Community/Comments/{postId:long}")]
    public async Task<IActionResult> PostComments(long postId, int page = 1)
    {
        var result = await _postService.GetCommentsAsync(postId, page, PageSize);

        if (!result.Success)
            return NotFound();

        var comments = result.Comments.Select(c => new CommentViewModel
        {
            Id = c.Id,
            UserId = c.UserId,
            UserDisplayName = c.UserDisplayName,
            UserAvatarUrl = c.UserAvatarUrl,
            UserType = Enum.TryParse<Domain.Enums.UserType>(c.UserType, out var ut) ? ut : Domain.Enums.UserType.Normal,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            ParentCommentId = c.ParentCommentId,
            Replies = c.Replies.Select(r => new CommentViewModel
            {
                Id = r.Id,
                UserId = r.UserId,
                UserDisplayName = r.UserDisplayName,
                UserAvatarUrl = r.UserAvatarUrl,
                UserType = Enum.TryParse<Domain.Enums.UserType>(r.UserType, out var rut) ? rut : Domain.Enums.UserType.Normal,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                ParentCommentId = r.ParentCommentId
            }).ToList()
        }).ToList();

        ViewBag.PostId = postId;
        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;

        return PartialView("_CommentList", comments);
    }
}
