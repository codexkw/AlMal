namespace AlMal.Domain.Entities;

public class Comment : BaseEntity
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string UserId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public long? ParentCommentId { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation
    public Post Post { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
