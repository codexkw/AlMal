using System.Security.Claims;
using System.Text.Json;
using AlMal.Application.DTOs.Api;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/academy")]
public class AcademyApiController : ControllerBase
{
    private readonly AlMalDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AcademyApiController(AlMalDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static List<int> ParseCompletedLessonIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<int>();

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    private static string SerializeCompletedLessonIds(List<int> ids) =>
        JsonSerializer.Serialize(ids);

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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var currentUserId = GetCurrentUserId();

        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        // Difficulty filter
        if (!string.IsNullOrEmpty(difficulty))
        {
            query = difficulty.ToLower() switch
            {
                "free" => query.Where(c => c.IsFree),
                "paid" => query.Where(c => !c.IsFree),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();

        var courses = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                TitleAr = c.TitleAr,
                DescriptionAr = c.DescriptionAr,
                ThumbnailUrl = c.ThumbnailUrl,
                LessonCount = c.Lessons.Count,
                TotalDurationMinutes = c.Lessons.Sum(l => l.DurationMinutes),
                IsFree = c.IsFree,
                Price = c.Price,
                EnrolledCount = c.EnrollmentCount
            })
            .ToListAsync();

        // Set enrollment info for the current user
        if (currentUserId != null && courses.Count > 0)
        {
            var courseIds = courses.Select(c => c.Id).ToList();
            var enrollments = await _db.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == currentUserId && courseIds.Contains(e.CourseId))
                .Select(e => new { e.CourseId, e.Progress })
                .ToListAsync();

            foreach (var course in courses)
            {
                var enrollment = enrollments.FirstOrDefault(e => e.CourseId == course.Id);
                if (enrollment != null)
                {
                    course.IsEnrolled = true;
                    course.ProgressPercent = enrollment.Progress;
                }
            }
        }

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
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
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<object>.Fail("UNAUTHORIZED", "غير مصرح"));

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.IsPublished);

        if (course == null)
            return NotFound(ApiResponse<object>.Fail("COURSE_NOT_FOUND", "الدورة غير موجودة"));

        // Check if already enrolled
        var existingEnrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == id);

        if (existingEnrollment != null)
            return Ok(ApiResponse<object>.Ok(new { message = "أنت مسجل بالفعل في هذه الدورة", courseId = id }));

        var enrollment = new Enrollment
        {
            UserId = currentUserId,
            CourseId = id,
            Progress = 0,
            CompletedLessonIds = "[]",
            EnrolledAt = DateTime.UtcNow
        };

        _db.Enrollments.Add(enrollment);

        // Increment enrollment count
        var courseEntity = await _db.Courses.FindAsync(id);
        if (courseEntity != null)
        {
            courseEntity.EnrollmentCount++;
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { message = "تم التسجيل بنجاح", courseId = id }));
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
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<LessonDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Quiz)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound(ApiResponse<LessonDto>.Fail("LESSON_NOT_FOUND", "الدرس غير موجود"));

        // Verify user is enrolled
        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == lesson.CourseId);

        if (enrollment == null)
            return StatusCode(403, ApiResponse<LessonDto>.Fail("NOT_ENROLLED", "يجب التسجيل في الدورة أولاً"));

        var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

        var dto = new LessonDto
        {
            Id = lesson.Id,
            TitleAr = lesson.TitleAr,
            ContentAr = lesson.ContentAr,
            VideoUrl = lesson.VideoUrl,
            SortOrder = lesson.SortOrder,
            DurationMinutes = lesson.DurationMinutes,
            CourseId = lesson.CourseId,
            CourseTitleAr = lesson.Course.TitleAr,
            HasQuiz = lesson.Quiz != null,
            QuizId = lesson.Quiz?.Id,
            IsCompleted = completedIds.Contains(lesson.Id)
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
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Unauthorized(ApiResponse<QuizResultDto>.Fail("UNAUTHORIZED", "غير مصرح"));

        var quiz = await _db.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null)
            return NotFound(ApiResponse<QuizResultDto>.Fail("QUIZ_NOT_FOUND", "الاختبار غير موجود"));

        // Verify enrolled
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == quiz.Lesson.CourseId);

        if (enrollment == null)
            return StatusCode(403, ApiResponse<QuizResultDto>.Fail("NOT_ENROLLED", "يجب التسجيل في الدورة أولاً"));

        // Calculate score
        int correctCount = 0;
        int totalCount = quiz.Questions.Count;

        foreach (var question in quiz.Questions)
        {
            if (submission.Answers.TryGetValue(question.Id, out int selectedIndex)
                && selectedIndex == question.CorrectIndex)
            {
                correctCount++;
            }
        }

        int score = totalCount > 0
            ? (int)Math.Round(correctCount * 100.0 / totalCount)
            : 0;

        bool passed = score >= quiz.PassingScore;

        var result = new QuizResultDto
        {
            Passed = passed,
            Score = score,
            PassPercentage = quiz.PassingScore,
            CorrectCount = correctCount,
            TotalCount = totalCount
        };

        if (passed)
        {
            // Mark lesson as completed
            var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

            if (!completedIds.Contains(quiz.LessonId))
            {
                completedIds.Add(quiz.LessonId);
                enrollment.CompletedLessonIds = SerializeCompletedLessonIds(completedIds);
            }

            // Recalculate progress
            var allLessonIds = await _db.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == quiz.Lesson.CourseId)
                .Select(l => l.Id)
                .ToListAsync();

            int totalLessons = allLessonIds.Count;
            enrollment.Progress = totalLessons > 0
                ? (int)Math.Round(completedIds.Count * 100.0 / totalLessons)
                : 0;

            // Check if ALL lessons completed
            bool courseCompleted = allLessonIds.All(lid => completedIds.Contains(lid));

            if (courseCompleted)
            {
                enrollment.Progress = 100;
                result.CourseCompleted = true;

                // Check if certificate already exists
                var existingCertificate = await _db.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == currentUserId && c.CourseId == quiz.Lesson.CourseId);

                if (existingCertificate == null)
                {
                    var certificate = new Certificate
                    {
                        UserId = currentUserId,
                        CourseId = quiz.Lesson.CourseId,
                        CertificateNumber = $"ALMAL-{quiz.Lesson.CourseId:D4}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        IssuedAt = DateTime.UtcNow
                    };

                    _db.Certificates.Add(certificate);
                    await _db.SaveChangesAsync();

                    result.CertificateId = certificate.Id;

                    // Upgrade user if paid course completed
                    if (!quiz.Lesson.Course.IsFree)
                    {
                        var user = await _userManager.FindByIdAsync(currentUserId);
                        if (user != null && user.UserType == UserType.Normal)
                        {
                            user.UserType = UserType.ProAnalyst;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }
                else
                {
                    result.CertificateId = existingCertificate.Id;
                }
            }

            await _db.SaveChangesAsync();
        }

        return Ok(ApiResponse<QuizResultDto>.Ok(result));
    }
}
