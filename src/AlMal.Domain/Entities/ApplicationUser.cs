using AlMal.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AlMal.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; } = UserType.Normal;
    public bool IsVerified { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public string? WhatsAppNumber { get; set; }
    public bool WhatsAppOptIn { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> PostLikes { get; set; } = new List<PostLike>();
    public ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
    public ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
    public ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
    public SimulationPortfolio? SimulationPortfolio { get; set; }
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
