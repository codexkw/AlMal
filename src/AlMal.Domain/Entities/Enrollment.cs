namespace AlMal.Domain.Entities;

public class Enrollment
{
    public string UserId { get; set; } = null!;
    public int CourseId { get; set; }
    public int Progress { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
