namespace AlMal.Domain.Entities;

public class UserFollow
{
    public string FollowerId { get; set; } = null!;
    public string FollowingId { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser Follower { get; set; } = null!;
    public ApplicationUser FollowingUser { get; set; } = null!;
}
