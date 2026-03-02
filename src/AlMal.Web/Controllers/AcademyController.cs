using System.Security.Claims;
using AlMal.Application.Interfaces;
using AlMal.Web.ViewModels.Academy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.Web.Controllers;

public class AcademyController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IQuizService _quizService;
    private readonly ICertificateService _certificateService;
    private const int PageSize = 9;

    public AcademyController(
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
    // GET /Academy — Course Catalog
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Index(string? difficulty, int page = 1)
    {
        var result = await _courseService.GetCourseCatalogAsync(difficulty, page, PageSize, GetCurrentUserId());

        var viewModel = new CourseCatalogViewModel
        {
            Courses = result.Courses.Select(c => new CourseCardViewModel
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
            }).ToList(),
            DifficultyFilter = difficulty,
            Page = result.Page,
            TotalPages = (int)Math.Ceiling(result.TotalCount / (double)PageSize),
            TotalCourses = result.TotalCount
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Course/{id} — Course Detail
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    public async Task<IActionResult> Course(int id)
    {
        var result = await _courseService.GetCourseDetailAsync(id, GetCurrentUserId());
        if (result == null)
            return NotFound();

        var viewModel = new CourseDetailViewModel
        {
            Id = result.Id,
            TitleAr = result.TitleAr,
            DescriptionAr = result.DescriptionAr,
            ThumbnailUrl = result.ThumbnailUrl,
            LessonCount = result.LessonCount,
            TotalDurationMinutes = result.TotalDurationMinutes,
            IsFree = result.IsFree,
            Price = result.Price,
            EnrolledCount = result.EnrolledCount,
            IsEnrolled = result.IsEnrolled,
            ProgressPercent = result.ProgressPercent,
            Lessons = result.Lessons.Select(l => new LessonListItemViewModel
            {
                Id = l.Id,
                TitleAr = l.TitleAr,
                SortOrder = l.SortOrder,
                DurationMinutes = l.DurationMinutes,
                HasQuiz = l.HasQuiz,
                IsCompleted = l.IsCompleted
            }).ToList()
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // POST /Academy/Enroll — Create Enrollment
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _courseService.EnrollAsync(courseId, userId);
        return RedirectToAction("Course", new { id = courseId });
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Lesson/{id} — Lesson Viewer
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Lesson(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _courseService.GetLessonAsync(id, userId);
        if (result == null)
            return NotFound();

        var viewModel = new LessonViewModel
        {
            Id = result.Id,
            TitleAr = result.TitleAr,
            ContentAr = result.ContentAr,
            VideoUrl = result.VideoUrl,
            SortOrder = result.SortOrder,
            IsCompleted = result.IsCompleted,
            CourseId = result.CourseId,
            CourseTitleAr = result.CourseTitleAr,
            HasQuiz = result.HasQuiz,
            QuizId = result.QuizId,
            PreviousLessonId = result.PreviousLessonId,
            NextLessonId = result.NextLessonId
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
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _courseService.CompleteLessonAsync(lessonId, userId);

        if (result.NextLessonId.HasValue)
            return RedirectToAction("Lesson", new { id = result.NextLessonId.Value });

        return RedirectToAction("Course", new { id = result.CourseId });
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Quiz/{lessonId} — Quiz Page
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Quiz(int lessonId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _quizService.GetQuizForLessonAsync(lessonId, userId);
        if (result == null)
            return RedirectToAction("Lesson", new { id = lessonId });

        var viewModel = new QuizViewModel
        {
            QuizId = result.QuizId,
            LessonId = result.LessonId,
            LessonTitleAr = result.LessonTitleAr,
            CourseTitleAr = result.CourseTitleAr,
            CourseId = result.CourseId,
            PassPercentage = result.PassPercentage,
            Questions = result.Questions.Select(q => new QuizQuestionViewModel
            {
                Id = q.Id,
                QuestionAr = q.QuestionAr,
                Options = q.Options,
                CorrectIndex = q.CorrectIndex
            }).ToList()
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // POST /Academy/SubmitQuiz — Submit Quiz Answers
    // ════════════════════════════════════════════════════════════

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuiz(int quizId, Dictionary<int, int> answers)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _quizService.SubmitQuizAsync(quizId, answers, userId);
        if (result == null)
            return NotFound();

        var viewModel = new QuizResultViewModel
        {
            Passed = result.Passed,
            Score = result.Score,
            PassPercentage = result.PassPercentage,
            CorrectCount = result.CorrectCount,
            TotalCount = result.TotalCount,
            CourseId = result.CourseId,
            CourseTitleAr = result.CourseTitleAr,
            LessonId = result.LessonId,
            LessonTitleAr = result.LessonTitleAr,
            CourseCompleted = result.CourseCompleted,
            CertificateId = result.CertificateId
        };

        return View("QuizResult", viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/Certificate/{id} — Certificate View
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Certificate(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var result = await _certificateService.GetCertificateAsync(id, userId);
        if (result == null)
            return NotFound();

        var viewModel = new CertificateViewModel
        {
            Id = result.Id,
            CourseTitleAr = result.CourseTitleAr,
            UserName = result.UserName,
            CompletionDate = result.CompletionDate,
            CertificateNumber = result.CertificateNumber
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
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var certificates = await _certificateService.GetUserCertificatesAsync(userId);

        var viewModel = new MyCertificatesViewModel
        {
            Certificates = certificates.Select(c => new CertificateViewModel
            {
                Id = c.Id,
                CourseTitleAr = c.CourseTitleAr,
                UserName = c.UserName,
                CompletionDate = c.CompletionDate,
                CertificateNumber = c.CertificateNumber
            }).ToList()
        };

        return View(viewModel);
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/DownloadCertificate/{id} — Download Certificate PDF
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> DownloadCertificate(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        // Verify ownership
        var cert = await _certificateService.GetCertificateAsync(id, userId);
        if (cert == null)
            return NotFound();

        var pdfBytes = await _certificateService.GenerateCertificatePdfAsync(id);
        if (pdfBytes.Length == 0)
            return NotFound();

        return File(pdfBytes, "application/pdf", $"AlMal-Certificate-{cert.CertificateNumber}.pdf");
    }

    // ════════════════════════════════════════════════════════════
    // GET /Academy/VerifyCertificate — Public Certificate Verification
    // ════════════════════════════════════════════════════════════

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCertificate(string certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            return Json(new { valid = false });

        var result = await _certificateService.VerifyCertificateAsync(certificateNumber);
        return Json(result);
    }
}
