using System.Text.RegularExpressions;
using AlMal.Application.DTOs.Api;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.Services;

public partial class PostService : IPostService
{
    private readonly AlMalDbContext _db;
    private readonly ILogger<PostService> _logger;

    private static readonly HashSet<string> AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly HashSet<string> AllowedVideoExtensions = [".mp4", ".webm"];
    private const long MaxImageSize = 5 * 1024 * 1024;   // 5 MB
    private const long MaxVideoSize = 50 * 1024 * 1024;   // 50 MB

    public PostService(AlMalDbContext db, ILogger<PostService> logger)
    {
        _db = db;
        _logger = logger;
    }

    [GeneratedRegex(@"\$([A-Z]+)")]
    private static partial Regex StockMentionRegex();

    public async Task<PostFeedResult> GetFeedAsync(string? currentUserId, string tab, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        IQueryable<Post> query = _db.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (tab == "following" && currentUserId != null)
        {
            var followingIds = _db.UserFollows
                .AsNoTracking()
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FollowingId);

            query = query.Where(p => followingIds.Contains(p.UserId));
        }

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
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
                    .ToList(),
                IsRepost = p.OriginalPostId != null,
                OriginalPostId = p.OriginalPostId,
                OriginalPost = p.OriginalPostId != null && p.OriginalPost != null
                    ? new PostDto
                    {
                        Id = p.OriginalPost.Id,
                        UserId = p.OriginalPost.UserId,
                        UserDisplayName = p.OriginalPost.User.DisplayName,
                        UserAvatarUrl = p.OriginalPost.User.AvatarUrl,
                        UserType = p.OriginalPost.User.UserType.ToString(),
                        Content = p.OriginalPost.Content,
                        ImageUrl = p.OriginalPost.ImageUrl,
                        VideoUrl = p.OriginalPost.VideoUrl,
                        LikeCount = p.OriginalPost.LikeCount,
                        CommentCount = p.OriginalPost.CommentCount,
                        RepostCount = p.OriginalPost.RepostCount,
                        CreatedAt = p.OriginalPost.CreatedAt,
                        StockMentions = p.OriginalPost.PostStockMentions
                            .Select(m => m.Stock.Symbol)
                            .ToList()
                    }
                    : null
            })
            .ToListAsync(ct);

        return PostFeedResult.From(posts, totalCount, page, totalPages);
    }

    public async Task<PostDto?> GetPostByIdAsync(long postId, string? currentUserId, CancellationToken ct = default)
    {
        return await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId && !p.IsDeleted)
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
                    .ToList(),
                IsRepost = p.OriginalPostId != null,
                OriginalPostId = p.OriginalPostId,
                OriginalPost = p.OriginalPostId != null && p.OriginalPost != null
                    ? new PostDto
                    {
                        Id = p.OriginalPost.Id,
                        UserId = p.OriginalPost.UserId,
                        UserDisplayName = p.OriginalPost.User.DisplayName,
                        UserAvatarUrl = p.OriginalPost.User.AvatarUrl,
                        UserType = p.OriginalPost.User.UserType.ToString(),
                        Content = p.OriginalPost.Content,
                        ImageUrl = p.OriginalPost.ImageUrl,
                        VideoUrl = p.OriginalPost.VideoUrl,
                        LikeCount = p.OriginalPost.LikeCount,
                        CommentCount = p.OriginalPost.CommentCount,
                        RepostCount = p.OriginalPost.RepostCount,
                        CreatedAt = p.OriginalPost.CreatedAt,
                        StockMentions = p.OriginalPost.PostStockMentions
                            .Select(m => m.Stock.Symbol)
                            .ToList()
                    }
                    : null
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PostCreateResult> CreatePostAsync(
        string userId, string content,
        Stream? imageStream, string? imageFileName,
        Stream? videoStream, string? videoFileName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return PostCreateResult.Fail("VALIDATION_ERROR", "محتوى المنشور مطلوب.");

        if (content.Length > 2000)
            return PostCreateResult.Fail("VALIDATION_ERROR", "محتوى المنشور يجب ألا يتجاوز 2000 حرف.");

        // Validate and save media files
        string? imageUrl = null;
        string? videoUrl = null;

        if (imageStream != null && imageFileName != null)
        {
            var ext = Path.GetExtension(imageFileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
                return PostCreateResult.Fail("VALIDATION_ERROR", "نوع الصورة غير مدعوم. الأنواع المسموحة: JPG, PNG, GIF, WebP");

            if (imageStream.Length > MaxImageSize)
                return PostCreateResult.Fail("VALIDATION_ERROR", "حجم الصورة يجب ألا يتجاوز 5 ميجابايت.");

            imageUrl = await SaveMediaFileAsync(imageStream, imageFileName, "images", ct);
        }

        if (videoStream != null && videoFileName != null)
        {
            var ext = Path.GetExtension(videoFileName).ToLowerInvariant();
            if (!AllowedVideoExtensions.Contains(ext))
                return PostCreateResult.Fail("VALIDATION_ERROR", "نوع الفيديو غير مدعوم. الأنواع المسموحة: MP4, WebM");

            if (videoStream.Length > MaxVideoSize)
                return PostCreateResult.Fail("VALIDATION_ERROR", "حجم الفيديو يجب ألا يتجاوز 50 ميجابايت.");

            videoUrl = await SaveMediaFileAsync(videoStream, videoFileName, "videos", ct);
        }

        var post = new Post
        {
            UserId = userId,
            Content = content.Trim(),
            ImageUrl = imageUrl,
            VideoUrl = videoUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(ct);

        // Parse $SYMBOL tags
        var matches = StockMentionRegex().Matches(content);
        if (matches.Count > 0)
        {
            var symbols = matches.Select(m => m.Groups[1].Value).Distinct().ToList();
            var stocks = await _db.Stocks
                .AsNoTracking()
                .Where(s => symbols.Contains(s.Symbol) && s.IsActive)
                .Select(s => new { s.Id, s.Symbol })
                .ToListAsync(ct);

            foreach (var stock in stocks)
            {
                _db.PostStockMentions.Add(new PostStockMention
                {
                    PostId = post.Id,
                    StockId = stock.Id
                });
            }

            if (stocks.Count > 0)
                await _db.SaveChangesAsync(ct);
        }

        // Increment user PostCount
        var user = await _db.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
        {
            user.PostCount++;
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Post {PostId} created by user {UserId}", post.Id, userId);

        return PostCreateResult.Created(post.Id);
    }

    public async Task<PostCreateResult> RepostAsync(string userId, long originalPostId, string? comment, CancellationToken ct = default)
    {
        var original = await _db.Posts.FirstOrDefaultAsync(p => p.Id == originalPostId && !p.IsDeleted, ct);
        if (original == null)
            return PostCreateResult.Fail("POST_NOT_FOUND", "المنشور الأصلي غير موجود.");

        // Prevent reposting own post
        if (original.UserId == userId)
            return PostCreateResult.Fail("CANNOT_REPOST_OWN", "لا يمكنك إعادة نشر منشورك.");

        // Prevent double repost
        var alreadyReposted = await _db.Posts
            .AsNoTracking()
            .AnyAsync(p => p.OriginalPostId == originalPostId && p.UserId == userId && !p.IsDeleted, ct);

        if (alreadyReposted)
            return PostCreateResult.Fail("ALREADY_REPOSTED", "لقد قمت بإعادة نشر هذا المنشور بالفعل.");

        var repost = new Post
        {
            UserId = userId,
            Content = string.IsNullOrWhiteSpace(comment) ? original.Content : comment.Trim(),
            OriginalPostId = originalPostId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(repost);
        original.RepostCount++;

        // Increment user PostCount
        var user = await _db.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
            user.PostCount++;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Post {PostId} reposted as {RepostId} by user {UserId}", originalPostId, repost.Id, userId);

        return PostCreateResult.Created(repost.Id);
    }

    public async Task<LikeResult> ToggleLikeAsync(string userId, long postId, CancellationToken ct = default)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
        if (post == null)
            return LikeResult.Fail("POST_NOT_FOUND", "المنشور غير موجود.");

        var existingLike = await _db.PostLikes
            .FirstOrDefaultAsync(pl => pl.UserId == userId && pl.PostId == postId, ct);

        bool isLiked;
        if (existingLike != null)
        {
            _db.PostLikes.Remove(existingLike);
            post.LikeCount = Math.Max(0, post.LikeCount - 1);
            isLiked = false;
        }
        else
        {
            _db.PostLikes.Add(new PostLike
            {
                UserId = userId,
                PostId = postId,
                CreatedAt = DateTime.UtcNow
            });
            post.LikeCount++;
            isLiked = true;
        }

        await _db.SaveChangesAsync(ct);
        return LikeResult.Toggled(isLiked, post.LikeCount);
    }

    public async Task<CommentResult> AddCommentAsync(string userId, long postId, string content, long? parentCommentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return CommentResult.Fail("VALIDATION_ERROR", "محتوى التعليق مطلوب.");

        if (content.Length > 500)
            return CommentResult.Fail("VALIDATION_ERROR", "التعليق يجب ألا يتجاوز 500 حرف.");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
        if (post == null)
            return CommentResult.Fail("POST_NOT_FOUND", "المنشور غير موجود.");

        if (parentCommentId.HasValue)
        {
            var parentExists = await _db.Comments
                .AsNoTracking()
                .AnyAsync(c => c.Id == parentCommentId.Value && c.PostId == postId && !c.IsDeleted, ct);

            if (!parentExists)
                return CommentResult.Fail("PARENT_NOT_FOUND", "التعليق الأصلي غير موجود.");
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = content.Trim(),
            ParentCommentId = parentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Comments.Add(comment);
        post.CommentCount++;
        await _db.SaveChangesAsync(ct);

        // Load user info for result
        var user = await _db.Users
            .AsNoTracking()
            .OfType<ApplicationUser>()
            .Where(u => u.Id == userId)
            .Select(u => new { u.DisplayName, u.AvatarUrl, u.UserType })
            .FirstOrDefaultAsync(ct);

        var commentDto = new CommentDto
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

        return CommentResult.Created(commentDto);
    }

    public async Task<CommentListResult> GetCommentsAsync(long postId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var postExists = await _db.Posts
            .AsNoTracking()
            .AnyAsync(p => p.Id == postId && !p.IsDeleted, ct);

        if (!postExists)
            return new CommentListResult { Success = false, ErrorCode = "POST_NOT_FOUND", ErrorMessage = "المنشور غير موجود." };

        var query = _db.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && !c.IsDeleted && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

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
                        Replies = []
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        return CommentListResult.From(comments, totalCount, page, totalPages);
    }

    public async Task<ServiceResult> DeletePostAsync(string userId, long postId, CancellationToken ct = default)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
        if (post == null)
            return ServiceResult.Fail("POST_NOT_FOUND", "المنشور غير موجود.");

        if (post.UserId != userId)
            return ServiceResult.Fail("FORBIDDEN", "لا يمكنك حذف هذا المنشور.");

        post.IsDeleted = true;
        post.UpdatedAt = DateTime.UtcNow;

        // If this was a repost, decrement original's RepostCount
        if (post.OriginalPostId.HasValue)
        {
            var original = await _db.Posts.FindAsync([post.OriginalPostId.Value], ct);
            if (original != null)
                original.RepostCount = Math.Max(0, original.RepostCount - 1);
        }

        // Decrement user PostCount
        var user = await _db.Users.OfType<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user != null)
            user.PostCount = Math.Max(0, user.PostCount - 1);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Post {PostId} deleted by user {UserId}", postId, userId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReportPostAsync(string userId, long postId, string? reason, CancellationToken ct = default)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, ct);
        if (post == null)
            return ServiceResult.Fail("POST_NOT_FOUND", "المنشور غير موجود.");

        post.IsFlagged = true;
        post.ReportReason = reason;
        post.ReportedByUserId = userId;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Post {PostId} reported by user {UserId}: {Reason}", postId, userId, reason ?? "(no reason)");

        return ServiceResult.Ok();
    }

    private static async Task<string> SaveMediaFileAsync(Stream stream, string fileName, string subfolder, CancellationToken ct)
    {
        var uploadsDir = Path.Combine("wwwroot", "uploads", "posts", subfolder);
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueName);

        using var fs = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fs, ct);

        return $"/uploads/posts/{subfolder}/{uniqueName}";
    }
}
