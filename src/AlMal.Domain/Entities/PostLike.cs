namespace AlMal.Domain.Entities;

public class PostLike
{
    public string UserId { get; set; } = null!;
    public long PostId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Post Post { get; set; } = null!;
}
