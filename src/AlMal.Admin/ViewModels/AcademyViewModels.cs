using System.ComponentModel.DataAnnotations;

namespace AlMal.Admin.ViewModels;

// ── Course List ──────────────────────────────────────────
public class CourseListViewModel
{
    public List<CourseListItemViewModel> Items { get; set; } = new List<CourseListItemViewModel>();
    public string? SearchTerm { get; set; }
    public string? PublishedFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class CourseListItemViewModel
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public bool IsFree { get; set; }
    public decimal? Price { get; set; }
    public bool IsPublished { get; set; }
    public int LessonCount { get; set; }
    public int EnrollmentCount { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Course Create/Edit ───────────────────────────────────
public class CourseEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الدورة مطلوب")]
    [MaxLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
    public string TitleAr { get; set; } = null!;

    [MaxLength(2000, ErrorMessage = "الحد الأقصى 2000 حرف")]
    public string? DescriptionAr { get; set; }

    [MaxLength(500)]
    public string? ThumbnailUrl { get; set; }

    public bool IsFree { get; set; } = true;

    [Range(0, 99999, ErrorMessage = "السعر يجب أن يكون بين 0 و 99999")]
    public decimal? Price { get; set; }

    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}

// ── Lesson List (per Course) ─────────────────────────────
public class LessonListViewModel
{
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public List<LessonListItemViewModel> Lessons { get; set; } = new List<LessonListItemViewModel>();
}

public class LessonListItemViewModel
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool HasQuiz { get; set; }
    public bool HasVideo { get; set; }
}

// ── Lesson Create/Edit ───────────────────────────────────
public class LessonEditViewModel
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string? CourseTitleAr { get; set; }

    [Required(ErrorMessage = "عنوان الدرس مطلوب")]
    [MaxLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
    public string TitleAr { get; set; } = null!;

    public string? ContentHtml { get; set; }

    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    [Range(0, 600, ErrorMessage = "المدة يجب أن تكون بين 0 و 600 دقيقة")]
    public int DurationMinutes { get; set; }

    public int SortOrder { get; set; }
}

// ── Quiz Builder (per Lesson) ────────────────────────────
public class QuizEditViewModel
{
    public int QuizId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitleAr { get; set; } = null!;
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;

    [Range(0, 100, ErrorMessage = "نسبة النجاح يجب أن تكون بين 0 و 100")]
    public int PassingScore { get; set; } = 70;

    public List<QuestionEditViewModel> Questions { get; set; } = new List<QuestionEditViewModel>();
}

public class QuestionEditViewModel
{
    public int Id { get; set; }
    public int QuizId { get; set; }

    [Required(ErrorMessage = "نص السؤال مطلوب")]
    [MaxLength(1000)]
    public string QuestionAr { get; set; } = null!;

    [Required(ErrorMessage = "الخيار الأول مطلوب")]
    public string Option1 { get; set; } = null!;

    [Required(ErrorMessage = "الخيار الثاني مطلوب")]
    public string Option2 { get; set; } = null!;

    [Required(ErrorMessage = "الخيار الثالث مطلوب")]
    public string Option3 { get; set; } = null!;

    [Required(ErrorMessage = "الخيار الرابع مطلوب")]
    public string Option4 { get; set; } = null!;

    [Range(0, 3, ErrorMessage = "فهرس الإجابة الصحيحة يجب أن يكون بين 0 و 3")]
    public int CorrectIndex { get; set; }
}

// ── Enrollment Analytics ─────────────────────────────────
public class EnrollmentStatsViewModel
{
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public double CompletionRate { get; set; }
    public double AverageProgress { get; set; }
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int TotalCertificates { get; set; }

    public List<CourseStatsViewModel> CourseStats { get; set; } = new List<CourseStatsViewModel>();
    public List<EnrollmentItemViewModel> RecentEnrollments { get; set; } = new List<EnrollmentItemViewModel>();
}

public class CourseStatsViewModel
{
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public int EnrollmentCount { get; set; }
    public int CompletedCount { get; set; }
    public double CompletionRate { get; set; }
    public double AverageProgress { get; set; }
    public int CertificateCount { get; set; }
}

public class EnrollmentItemViewModel
{
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserEmail { get; set; }
    public int CourseId { get; set; }
    public string CourseTitleAr { get; set; } = null!;
    public int Progress { get; set; }
    public DateTime EnrolledAt { get; set; }
}
