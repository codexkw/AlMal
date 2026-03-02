using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class Notification : BaseEntity
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
