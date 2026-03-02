using System.Security.Claims;
using AlMal.Application.DTOs.Api;
using AlMal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/academy")]
public class AcademyApiController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IQuizService _quizService;
    private readonly ICertificateService _certificateService;

    public AcademyApiController(
        ICourseService courseService,
        IQuizService quizService,
        ICertificateService certificateService)
    {
        _courseService = courseService;
        _quizService = quizService;
        _certificateService = certificateService;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/academy/courses — Course Catalog
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/academy/courses — Paginated course catalog
    /// </summary>
    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? difficulty,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _courseService.GetCourseCatalogAsync(difficulty, page, pageSize, GetCurrentUserId());

        var courses = result.Courses.Select(c => new CourseDto
        {
            Id = c.Id,
            TitleAr = c.TitleAr,
            DescriptionAr = c.DescriptionAr,
            ThumbnailUrl = c.ThumbnailUrl,
            LessonCount = c.LessonCount,
            TotalDurationMinutes = c.TotalDurationMinutes,
            IsFree = c.IsFree,
            Price = c.Price,
            EnrolledCount = c.EnrolledCount,
            IsEnrolled = c.IsEnrolled,
            ProgressPercent = c.ProgressPercent
        }).ToList();

        var pagination = new PaginationInfo
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };

        return Ok(ApiResponse<List<CourseDto>>.Ok(courses, pagination));
    }

    // ════════════════════════════════════════════════════════════
    // POST /api/v1/academy/courses/{id}/enroll — Enroll in Course
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/v1/academy/courses/{id}/enroll — Enroll in a course
    /// </summary>
    [Authorize]
    [HttpPost("courses/{id:int}/enroll")]
    public async Task<IActionResult> Enroll(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _courseService.EnrollAsync(id, userId);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Fail("COURSE_NOT_FOUND", result.Message ?? "الدورة غير موجودة"));

        return Ok(ApiResponse<object>.Ok(new { message = result.Message, courseId = id }));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/academy/lessons/{id} — Lesson Content
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/academy/lessons/{id} — Get lesson content
    /// </summary>
    [Authorize]
    [HttpGet("lessons/{id:int}")]
    public async Task<IActionResult> GetLesson(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<LessonDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _courseService.GetLessonAsync(id, userId);

        if (result == null)
            return NotFound(ApiResponse<LessonDto>.Fail("LESSON_NOT_FOUND", "الدرس غير موجود أو غير مسجل في الدورة"));

        var dto = new LessonDto
        {
            Id = result.Id,
            TitleAr = result.TitleAr,
            ContentAr = result.ContentAr,
            VideoUrl = result.VideoUrl,
            SortOrder = result.SortOrder,
            DurationMinutes = 0,
            CourseId = result.CourseId,
            CourseTitleAr = result.CourseTitleAr,
            HasQuiz = result.HasQuiz,
            QuizId = result.QuizId,
            IsCompleted = result.IsCompleted
        };

        return Ok(ApiResponse<LessonDto>.Ok(dto));
    }

    // ════════════════════════════════════════════════════════════
    // POST /api/v1/academy/quizzes/{id}/submit — Submit Quiz
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// POST /api/v1/academy/quizzes/{id}/submit — Submit quiz answers
    /// </summary>
    [Authorize]
    [HttpPost("quizzes/{id:int}/submit")]
    public async Task<IActionResult> SubmitQuiz(int id, [FromBody] QuizSubmissionDto submission)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<QuizResultDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var result = await _quizService.SubmitQuizAsync(id, submission.Answers, userId);

        if (result == null)
            return NotFound(ApiResponse<QuizResultDto>.Fail("QUIZ_NOT_FOUND", "الاختبار غير موجود أو غير مسجل في الدورة"));

        var dto = new QuizResultDto
        {
            Passed = result.Passed,
            Score = result.Score,
            PassPercentage = result.PassPercentage,
            CorrectCount = result.CorrectCount,
            TotalCount = result.TotalCount,
            CourseCompleted = result.CourseCompleted,
            CertificateId = result.CertificateId
        };

        return Ok(ApiResponse<QuizResultDto>.Ok(dto));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/academy/courses/{id}/progress — Course Progress
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/academy/courses/{id}/progress — Get user's progress in course
    /// </summary>
    [Authorize]
    [HttpGet("courses/{id:int}/progress")]
    public async Task<IActionResult> GetProgress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var course = await _courseService.GetCourseDetailAsync(id, userId);
        if (course == null)
            return NotFound(ApiResponse<object>.Fail("COURSE_NOT_FOUND", "الدورة غير موجودة"));

        var completedLessons = course.Lessons.Count(l => l.IsCompleted);

        return Ok(ApiResponse<object>.Ok(new
        {
            progress = course.ProgressPercent,
            completedLessons,
            totalLessons = course.LessonCount
        }));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/academy/certificates — User's Certificates
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/academy/certificates — Get user's certificates
    /// </summary>
    [Authorize]
    [HttpGet("certificates")]
    public async Task<IActionResult> GetCertificates()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<List<CertificateDetail>>.Fail("UNAUTHORIZED", "غير مصرح"));

        var certificates = await _certificateService.GetUserCertificatesAsync(userId);
        return Ok(ApiResponse<List<CertificateDetail>>.Ok(certificates));
    }

    // ════════════════════════════════════════════════════════════
    // GET /api/v1/academy/certificates/{certificateNumber}/verify
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// GET /api/v1/academy/certificates/{certificateNumber}/verify — Verify certificate
    /// </summary>
    [HttpGet("certificates/{certificateNumber}/verify")]
    public async Task<IActionResult> VerifyCertificate(string certificateNumber)
    {
        var result = await _certificateService.VerifyCertificateAsync(certificateNumber);
        return Ok(ApiResponse<CertificateVerifyResult>.Ok(result));
    }
}
