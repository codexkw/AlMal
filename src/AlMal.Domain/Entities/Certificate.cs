namespace AlMal.Domain.Entities;

public class Certificate : BaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int CourseId { get; set; }
    public string CertificateNumber { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public string? PdfUrl { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
