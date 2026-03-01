namespace AlMal.Domain.Entities;

public class Course : BaseEntity
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFree { get; set; } = true;
    public decimal? Price { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public int LessonCount { get; set; }
    public int EnrollmentCount { get; set; }

    // Navigation
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
