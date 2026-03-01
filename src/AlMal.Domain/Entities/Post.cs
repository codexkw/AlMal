namespace AlMal.Domain.Entities;

public class Post : BaseEntity
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public int RepostCount { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsFlagged { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<PostStockMention> PostStockMentions { get; set; } = new List<PostStockMention>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
}
