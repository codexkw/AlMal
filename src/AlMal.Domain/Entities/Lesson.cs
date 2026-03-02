namespace AlMal.Domain.Entities;

public class Lesson : BaseEntity
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }

    // Navigation
    public Course Course { get; set; } = null!;
    public Quiz? Quiz { get; set; }
}
