namespace AlMal.Application.DTOs.Api;

// ── Course DTO ───────────────────────────────────────────────────

public class CourseDto
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? DescriptionAr { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int LessonCount { get; set; }
    public int TotalDurationMinutes { get; set; }
    public bool IsFree { get; set; }
    public decimal? Price { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsEnrolled { get; set; }
    public int ProgressPercent { get; set; }
}

// ── Lesson DTO ───────────────────────────────────────────────────

public class LessonDto
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public bool HasQuiz { get; set; }
    public int? QuizId { get; set; }
    public bool IsCompleted { get; set; }
}

// ── Quiz Submission DTO ──────────────────────────────────────────

public class QuizSubmissionDto
{
    public Dictionary<int, int> Answers { get; set; } = new Dictionary<int, int>();
}

// ── Quiz Result DTO ──────────────────────────────────────────────

public class QuizResultDto
{
    public bool Passed { get; set; }
    public int Score { get; set; }
    public int PassPercentage { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public bool CourseCompleted { get; set; }
    public int? CertificateId { get; set; }
}
