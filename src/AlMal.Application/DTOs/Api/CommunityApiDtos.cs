namespace AlMal.Application.DTOs.Api;

public class PostDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public string UserType { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int RepostCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> StockMentions { get; set; } = [];

    // Repost fields
    public bool IsRepost { get; set; }
    public long? OriginalPostId { get; set; }
    public PostDto? OriginalPost { get; set; }
}

public class CommentDto
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public string UserType { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public long? ParentCommentId { get; set; }
    public List<CommentDto> Replies { get; set; } = [];
}

public class UserProfileDto
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string UserType { get; set; } = null!;
    public bool IsVerified { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public bool IsFollowing { get; set; }
}

public class CreatePostDto
{
    public string Content { get; set; } = null!;
    public List<string>? StockMentions { get; set; }
}

public class RepostDto
{
    public long OriginalPostId { get; set; }
    public string? Comment { get; set; }
}

public class CreateCommentDto
{
    public string Content { get; set; } = null!;
    public long? ParentCommentId { get; set; }
}

public class ReportPostDto
{
    public string? Reason { get; set; }
}
