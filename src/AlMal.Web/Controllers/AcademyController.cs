using System.Security.Claims;
using System.Text.Json;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.Academy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class AcademyController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AlMalDbContext _context;
    private const int PageSize = 9;

    public AcademyController(
        UserManager<ApplicationUser> userManager,
        AlMalDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // ── Helpers ─────────────────────────────────────────────────

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    private static List<int> ParseCompletedLessonIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<int>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeCompletedLessonIds(List<int> ids) =>
        JsonSerializer.Serialize(ids);

    // ════════════════════════════════════════════════════════════
    // GET /Academy — Course Catalog
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(string? difficulty, int page = 1)
    {
        if (page < 1) page = 1;

        var currentUserId = GetCurrentUserId();

        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        // Difficulty filter — IsFree is the proxy: free = beginner, paid = advanced
        if (!string.IsNullOrEmpty(difficulty))
        {
            query = difficulty.ToLower() switch
            {
                "free" => query.Where(c => c.IsFree),
                "paid" => query.Where(c => !c.IsFree),
                _ => query
            };
        }

        var totalCourses = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCourses / (double)PageSize);

        var courses = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CourseCardViewModel
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
            var enrollments = await _context.Enrollments
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

        var viewModel = new CourseCatalogViewModel
        {
            Courses = courses,
            DifficultyFilter = difficulty,
            Page = page,
            TotalPages = totalPages,
            TotalCourses = totalCourses
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Course/{id} — Course Detail
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Course(int id)
    {
        var currentUserId = GetCurrentUserId();

        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id && c.IsPublished)
            .Select(c => new CourseDetailViewModel
            {
                Id = c.Id,
                TitleAr = c.TitleAr,
                DescriptionAr = c.DescriptionAr,
                ThumbnailUrl = c.ThumbnailUrl,
                LessonCount = c.Lessons.Count,
                TotalDurationMinutes = c.Lessons.Sum(l => l.DurationMinutes),
                IsFree = c.IsFree,
                Price = c.Price,
                EnrolledCount = c.EnrollmentCount,
                Lessons = c.Lessons
                    .OrderBy(l => l.SortOrder)
                    .Select(l => new LessonListItemViewModel
                    {
                        Id = l.Id,
                        TitleAr = l.TitleAr,
                        SortOrder = l.SortOrder,
                        DurationMinutes = l.DurationMinutes,
                        HasQuiz = l.Quiz != null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        // Enrollment status
        if (currentUserId != null)
        {
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == id);

            if (enrollment != null)
            {
                course.IsEnrolled = true;
                course.ProgressPercent = enrollment.Progress;

                var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);
                foreach (var lesson in course.Lessons)
                {
                    lesson.IsCompleted = completedIds.Contains(lesson.Id);
                }
            }
        }

        return View(course);
    }

    // ════════════════════════════════════════════════════════════
    // POST /Academy/Enroll — Create Enrollment
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished);

        if (course == null)
            return NotFound();

        // Check if already enrolled
        var existingEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == courseId);

        if (existingEnrollment != null)
            return RedirectToAction("Course", new { id = courseId });

        var enrollment = new Enrollment
        {
            UserId = currentUserId,
            CourseId = courseId,
            Progress = 0,
            CompletedLessonIds = "[]",
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);

        // Increment enrollment count on the course
        var courseEntity = await _context.Courses.FindAsync(courseId);
        if (courseEntity != null)
        {
            courseEntity.EnrollmentCount++;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Course", new { id = courseId });
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Lesson/{id} — Lesson Viewer
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Lesson(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Quiz)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound();

        // Verify user is enrolled
        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == lesson.CourseId);

        if (enrollment == null)
            return RedirectToAction("Course", new { id = lesson.CourseId });

        var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

        // Get all lessons for prev/next navigation
        var allLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == lesson.CourseId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.Id)
            .ToListAsync();

        var currentIndex = allLessons.IndexOf(id);

        var viewModel = new LessonViewModel
        {
            Id = lesson.Id,
            TitleAr = lesson.TitleAr,
            ContentAr = lesson.ContentAr,
            VideoUrl = lesson.VideoUrl,
            SortOrder = lesson.SortOrder,
            IsCompleted = completedIds.Contains(lesson.Id),
            CourseId = lesson.CourseId,
            CourseTitleAr = lesson.Course.TitleAr,
            HasQuiz = lesson.Quiz != null,
            QuizId = lesson.Quiz?.Id,
            PreviousLessonId = currentIndex > 0 ? allLessons[currentIndex - 1] : null,
            NextLessonId = currentIndex < allLessons.Count - 1 ? allLessons[currentIndex + 1] : null
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // POST /Academy/CompleteLesson — Mark Lesson Done
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLesson(int lessonId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == lesson.CourseId);

        if (enrollment == null)
            return RedirectToAction("Course", new { id = lesson.CourseId });

        var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

        if (!completedIds.Contains(lessonId))
        {
            completedIds.Add(lessonId);
            enrollment.CompletedLessonIds = SerializeCompletedLessonIds(completedIds);

            // Recalculate progress
            var totalLessons = await _context.Lessons
                .AsNoTracking()
                .CountAsync(l => l.CourseId == lesson.CourseId);

            enrollment.Progress = totalLessons > 0
                ? (int)Math.Round(completedIds.Count * 100.0 / totalLessons)
                : 0;

            await _context.SaveChangesAsync();
        }

        // If the lesson has a next lesson, go there; otherwise back to course
        var nextLesson = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == lesson.CourseId && l.SortOrder > lesson.SortOrder)
            .OrderBy(l => l.SortOrder)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync();

        if (nextLesson.HasValue)
            return RedirectToAction("Lesson", new { id = nextLesson.Value });

        return RedirectToAction("Course", new { id = lesson.CourseId });
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Quiz/{lessonId} — Quiz Page
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Quiz(int lessonId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        // Verify enrolled
        var isEnrolled = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.UserId == currentUserId && e.CourseId == lesson.CourseId);

        if (!isEnrolled)
            return RedirectToAction("Course", new { id = lesson.CourseId });

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.LessonId == lessonId);

        if (quiz == null)
            return RedirectToAction("Lesson", new { id = lessonId });

        var viewModel = new QuizViewModel
        {
            QuizId = quiz.Id,
            LessonId = lessonId,
            LessonTitleAr = lesson.TitleAr,
            CourseTitleAr = lesson.Course.TitleAr,
            CourseId = lesson.CourseId,
            PassPercentage = quiz.PassingScore,
            Questions = quiz.Questions
                .Select(q => new QuizQuestionViewModel
                {
                    Id = q.Id,
                    QuestionAr = q.QuestionAr,
                    Options = DeserializeOptions(q.Options),
                    CorrectIndex = q.CorrectIndex
                })
                .ToList()
        };

        return View(viewModel);
    }

    private static List<string> DeserializeOptions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    // ════════════════════════════════════════════════════════════
    // POST /Academy/SubmitQuiz — Submit Quiz Answers
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> answers)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null)
            return NotFound();

        // Verify enrolled
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == currentUserId && e.CourseId == quiz.Lesson.CourseId);

        if (enrollment == null)
            return RedirectToAction("Course", new { id = quiz.Lesson.CourseId });

        // Calculate score
        int correctCount = 0;
        int totalCount = quiz.Questions.Count;

        foreach (var question in quiz.Questions)
        {
            if (answers.TryGetValue(question.Id, out int selectedIndex)
                && selectedIndex == question.CorrectIndex)
            {
                correctCount++;
            }
        }

        int score = totalCount > 0
            ? (int)Math.Round(correctCount * 100.0 / totalCount)
            : 0;

        bool passed = score >= quiz.PassingScore;

        var result = new QuizResultViewModel
        {
            Passed = passed,
            Score = score,
            PassPercentage = quiz.PassingScore,
            CorrectCount = correctCount,
            TotalCount = totalCount,
            CourseId = quiz.Lesson.CourseId,
            CourseTitleAr = quiz.Lesson.Course.TitleAr,
            LessonId = quiz.LessonId,
            LessonTitleAr = quiz.Lesson.TitleAr
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
            var allLessonIds = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == quiz.Lesson.CourseId)
                .Select(l => l.Id)
                .ToListAsync();

            int totalLessons = allLessonIds.Count;
            enrollment.Progress = totalLessons > 0
                ? (int)Math.Round(completedIds.Count * 100.0 / totalLessons)
                : 0;

            // Check if ALL lessons in the course are completed
            bool courseCompleted = allLessonIds.All(lid => completedIds.Contains(lid));

            if (courseCompleted)
            {
                enrollment.Progress = 100;
                result.CourseCompleted = true;

                // Check if certificate already exists
                var existingCertificate = await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == currentUserId && c.CourseId == quiz.Lesson.CourseId);

                if (existingCertificate == null)
                {
                    // Generate certificate
                    var certificate = new Certificate
                    {
                        UserId = currentUserId,
                        CourseId = quiz.Lesson.CourseId,
                        CertificateNumber = $"ALMAL-{quiz.Lesson.CourseId:D4}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        IssuedAt = DateTime.UtcNow
                    };

                    _context.Certificates.Add(certificate);
                    await _context.SaveChangesAsync();

                    result.CertificateId = certificate.Id;

                    // If course grants ProAnalyst, upgrade user's UserType
                    // Check if this is a course that should grant ProAnalyst
                    // (for now, completing any paid course grants ProAnalyst if user is Normal)
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

            await _context.SaveChangesAsync();
        }

        return View("QuizResult", result);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Certificate/{id} — Certificate View
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Certificate(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var certificate = await _context.Certificates
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == currentUserId);

        if (certificate == null)
            return NotFound();

        var viewModel = new CertificateViewModel
        {
            Id = certificate.Id,
            CourseTitleAr = certificate.Course.TitleAr,
            UserName = certificate.User.DisplayName,
            CompletionDate = certificate.IssuedAt,
            CertificateNumber = certificate.CertificateNumber
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/MyCertificates — List User's Certificates
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> MyCertificates()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        var certificates = await _context.Certificates
            .AsNoTracking()
            .Where(c => c.UserId == currentUserId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CertificateViewModel
            {
                Id = c.Id,
                CourseTitleAr = c.Course.TitleAr,
                UserName = c.User.DisplayName,
                CompletionDate = c.IssuedAt,
                CertificateNumber = c.CertificateNumber
            })
            .ToListAsync();

        var viewModel = new MyCertificatesViewModel
        {
            Certificates = certificates
        };

        return View(viewModel);
    }
}
