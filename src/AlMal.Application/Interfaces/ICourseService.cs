namespace AlMal.Application.Interfaces;

public class CourseCatalogResult
{
    public List<CourseSummary> Courses { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CourseSummary
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

public class CourseDetailResult
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
    public List<LessonSummary> Lessons { get; set; } = [];
}

public class LessonSummary
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public bool HasQuiz { get; set; }
    public bool IsCompleted { get; set; }
}

public class LessonDetailResult
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? ContentAr { get; set; }
    public string? VideoUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsCompleted { get; set; }
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public bool HasQuiz { get; set; }
    public int? QuizId { get; set; }
    public int? PreviousLessonId { get; set; }
    public int? NextLessonId { get; set; }
}

public class EnrollResult
{
    public bool Success { get; set; }
    public bool AlreadyEnrolled { get; set; }
    public int CourseId { get; set; }
    public string? Message { get; set; }
}

public class CompleteLessonResult
{
    public bool Success { get; set; }
    public int? NextLessonId { get; set; }
    public int CourseId { get; set; }
}

public interface ICourseService
{
    Task<CourseCatalogResult> GetCourseCatalogAsync(string? difficulty, int page, int pageSize, string? userId, CancellationToken ct = default);
    Task<CourseDetailResult?> GetCourseDetailAsync(int courseId, string? userId, CancellationToken ct = default);
    Task<LessonDetailResult?> GetLessonAsync(int lessonId, string userId, CancellationToken ct = default);
    Task<EnrollResult> EnrollAsync(int courseId, string userId, CancellationToken ct = default);
    Task<CompleteLessonResult> CompleteLessonAsync(int lessonId, string userId, CancellationToken ct = default);
}
