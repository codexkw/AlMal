using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Community;

public class CommunityFeedViewModel
{
    public List<PostCardViewModel> Posts { get; set; } = [];
    public string ActiveTab { get; set; } = "general"; // "general" or "following"
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
}

public class PostCardViewModel
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public UserType UserType { get; set; }
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int RepostCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StockMentionTag> StockMentions { get; set; } = [];
    public List<CommentViewModel> RecentComments { get; set; } = [];
}

public class StockMentionTag
{
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
}

public class CommentViewModel
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserAvatarUrl { get; set; }
    public UserType UserType { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public long? ParentCommentId { get; set; }
    public List<CommentViewModel> Replies { get; set; } = [];
}

public class CreatePostRequest
{
    public string Content { get; set; } = null!;
}
