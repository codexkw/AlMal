using AlMal.Application.DTOs.Api;

namespace AlMal.Application.Interfaces;

public interface IPostService
{
    Task<PostFeedResult> GetFeedAsync(string? currentUserId, string tab, int page, int pageSize, CancellationToken ct = default);
    Task<PostDto?> GetPostByIdAsync(long postId, string? currentUserId, CancellationToken ct = default);
    Task<PostCreateResult> CreatePostAsync(string userId, string content, Stream? imageStream, string? imageFileName, Stream? videoStream, string? videoFileName, CancellationToken ct = default);
    Task<PostCreateResult> RepostAsync(string userId, long originalPostId, string? comment, CancellationToken ct = default);
    Task<LikeResult> ToggleLikeAsync(string userId, long postId, CancellationToken ct = default);
    Task<CommentResult> AddCommentAsync(string userId, long postId, string content, long? parentCommentId, CancellationToken ct = default);
    Task<CommentListResult> GetCommentsAsync(long postId, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceResult> DeletePostAsync(string userId, long postId, CancellationToken ct = default);
    Task<ServiceResult> ReportPostAsync(string userId, long postId, string? reason, CancellationToken ct = default);
}

public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public class PostCreateResult : ServiceResult
{
    public long PostId { get; set; }
    public PostDto? Post { get; set; }

    public static PostCreateResult Created(long postId, PostDto? post = null) => new() { Success = true, PostId = postId, Post = post };
    public new static PostCreateResult Fail(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public class LikeResult : ServiceResult
{
    public bool IsLiked { get; set; }
    public int LikeCount { get; set; }

    public static LikeResult Toggled(bool isLiked, int likeCount) => new() { Success = true, IsLiked = isLiked, LikeCount = likeCount };
    public new static LikeResult Fail(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public class CommentResult : ServiceResult
{
    public CommentDto? Comment { get; set; }

    public static CommentResult Created(CommentDto comment) => new() { Success = true, Comment = comment };
    public new static CommentResult Fail(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

public class CommentListResult : ServiceResult
{
    public List<CommentDto> Comments { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }

    public static CommentListResult From(List<CommentDto> comments, int totalCount, int page, int totalPages) =>
        new() { Success = true, Comments = comments, TotalCount = totalCount, Page = page, TotalPages = totalPages };
}

public class PostFeedResult : ServiceResult
{
    public List<PostDto> Posts { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }

    public static PostFeedResult From(List<PostDto> posts, int totalCount, int page, int totalPages) =>
        new() { Success = true, Posts = posts, TotalCount = totalCount, Page = page, TotalPages = totalPages };
}
