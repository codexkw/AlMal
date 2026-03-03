using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class BadgeRequest : BaseEntity
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public UserType RequestedType { get; set; }
    public string? Justification { get; set; }
    public string? CertificateUrl { get; set; }
    public BadgeRequestStatus Status { get; set; } = BadgeRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
