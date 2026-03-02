namespace AlMal.Web.ViewModels.Academy;

// ── Course Catalog (Index) ──────────────────────────────────────

public class CourseCatalogViewModel
{
    public List<CourseCardViewModel> Courses { get; set; } = [];
    public string? DifficultyFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCourses { get; set; }
}

public class CourseCardViewModel
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

// ── Course Detail ───────────────────────────────────────────────

public class CourseDetailViewModel
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
    public List<LessonListItemViewModel> Lessons { get; set; } = [];
}

public class LessonListItemViewModel
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public int SortOrder { get; set; }
    public int DurationMinutes { get; set; }
    public bool HasQuiz { get; set; }
    public bool IsCompleted { get; set; }
}

// ── Lesson Viewer ───────────────────────────────────────────────

public class LessonViewModel
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

    // Navigation helpers
    public int? PreviousLessonId { get; set; }
    public int? NextLessonId { get; set; }
}

// ── Quiz ────────────────────────────────────────────────────────

public class QuizViewModel
{
    public int QuizId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitleAr { get; set; } = null!;
    public string CourseTitleAr { get; set; } = null!;
    public int CourseId { get; set; }
    public int PassPercentage { get; set; }
    public List<QuizQuestionViewModel> Questions { get; set; } = [];
}

public class QuizQuestionViewModel
{
    public int Id { get; set; }
    public string QuestionAr { get; set; } = null!;
    public List<string> Options { get; set; } = [];
    public int CorrectIndex { get; set; }
}

public class QuizResultViewModel
{
    public bool Passed { get; set; }
    public int Score { get; set; }
    public int PassPercentage { get; set; }
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public int LessonId { get; set; }
    public string LessonTitleAr { get; set; } = null!;
    public bool CourseCompleted { get; set; }
    public int? CertificateId { get; set; }
}

// ── Certificate ─────────────────────────────────────────────────

public class CertificateViewModel
{
    public int Id { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public DateTime CompletionDate { get; set; }
    public string CertificateNumber { get; set; } = null!;
}

public class MyCertificatesViewModel
{
    public List<CertificateViewModel> Certificates { get; set; } = [];
}
