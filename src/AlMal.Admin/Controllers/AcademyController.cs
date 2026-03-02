using System.Text.Json;
using AlMal.Admin.ViewModels;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AcademyController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<AcademyController> _logger;
    private const int PageSize = 20;

    public AcademyController(AlMalDbContext context, ILogger<AcademyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════
    //  COURSE CRUD
    // ══════════════════════════════════════════════════════════

    public async Task<IActionResult> Index(string? search, string? published, int page = 1)
    {
        var query = _context.Courses
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.TitleAr.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(published))
        {
            if (published == "true")
                query = query.Where(c => c.IsPublished);
            else if (published == "false")
                query = query.Where(c => !c.IsPublished);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CourseListItemViewModel
            {
                Id = c.Id,
                TitleAr = c.TitleAr,
                IsFree = c.IsFree,
                Price = c.Price,
                IsPublished = c.IsPublished,
                LessonCount = c.LessonCount,
                EnrollmentCount = c.EnrollmentCount,
                SortOrder = c.SortOrder,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        var viewModel = new CourseListViewModel
        {
            Items = items,
            SearchTerm = search,
            PublishedFilter = published,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    public IActionResult Create()
    {
        var viewModel = new CourseEditViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var course = new Course
        {
            TitleAr = model.TitleAr,
            DescriptionAr = model.DescriptionAr,
            ThumbnailUrl = model.ThumbnailUrl,
            IsFree = model.IsFree,
            Price = model.IsFree ? null : model.Price,
            SortOrder = model.SortOrder,
            IsPublished = model.IsPublished
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Course created: ID {Id}, Title: {Title}", course.Id, course.TitleAr);
        TempData["Success"] = "تم إنشاء الدورة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        var viewModel = new CourseEditViewModel
        {
            Id = course.Id,
            TitleAr = course.TitleAr,
            DescriptionAr = course.DescriptionAr,
            ThumbnailUrl = course.ThumbnailUrl,
            IsFree = course.IsFree,
            Price = course.Price,
            SortOrder = course.SortOrder,
            IsPublished = course.IsPublished
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CourseEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var course = await _context.Courses.FindAsync(model.Id);
        if (course == null)
            return NotFound();

        course.TitleAr = model.TitleAr;
        course.DescriptionAr = model.DescriptionAr;
        course.ThumbnailUrl = model.ThumbnailUrl;
        course.IsFree = model.IsFree;
        course.Price = model.IsFree ? null : model.Price;
        course.SortOrder = model.SortOrder;
        course.IsPublished = model.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Course updated: ID {Id}, Title: {Title}", course.Id, course.TitleAr);
        TempData["Success"] = "تم تحديث الدورة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
            return NotFound();

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Course deleted: ID {Id}, Title: {Title}", id, course.TitleAr);
        TempData["Success"] = "تم حذف الدورة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    // ══════════════════════════════════════════════════════════
    //  LESSON MANAGEMENT
    // ══════════════════════════════════════════════════════════

    public async Task<IActionResult> Lessons(int id)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null)
            return NotFound();

        var lessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == id)
            .OrderBy(l => l.SortOrder)
            .Select(l => new LessonListItemViewModel
            {
                Id = l.Id,
                TitleAr = l.TitleAr,
                DurationMinutes = l.DurationMinutes,
                SortOrder = l.SortOrder,
                HasQuiz = l.Quiz != null,
                HasVideo = l.VideoUrl != null
            })
            .ToListAsync();

        var viewModel = new LessonListViewModel
        {
            CourseId = course.Id,
            CourseTitleAr = course.TitleAr,
            Lessons = lessons
        };

        return View(viewModel);
    }

    public async Task<IActionResult> LessonCreate(int courseId)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return NotFound();

        var maxSortOrder = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .MaxAsync(l => (int?)l.SortOrder) ?? 0;

        var viewModel = new LessonEditViewModel
        {
            CourseId = courseId,
            CourseTitleAr = course.TitleAr,
            SortOrder = maxSortOrder + 1
        };

        return View("LessonEdit", viewModel);
    }

    public async Task<IActionResult> LessonEdit(int id)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound();

        var viewModel = new LessonEditViewModel
        {
            Id = lesson.Id,
            CourseId = lesson.CourseId,
            CourseTitleAr = lesson.Course.TitleAr,
            TitleAr = lesson.TitleAr,
            ContentHtml = lesson.ContentAr,
            VideoUrl = lesson.VideoUrl,
            DurationMinutes = lesson.DurationMinutes,
            SortOrder = lesson.SortOrder
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LessonSave(LessonEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("LessonEdit", model);
        }

        if (model.Id == 0)
        {
            // Create new lesson
            var lesson = new Lesson
            {
                CourseId = model.CourseId,
                TitleAr = model.TitleAr,
                ContentAr = model.ContentHtml,
                VideoUrl = model.VideoUrl,
                DurationMinutes = model.DurationMinutes,
                SortOrder = model.SortOrder
            };

            _context.Lessons.Add(lesson);

            // Update course lesson count
            var course = await _context.Courses.FindAsync(model.CourseId);
            if (course != null)
            {
                course.LessonCount = await _context.Lessons.CountAsync(l => l.CourseId == model.CourseId) + 1;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lesson created: ID {Id} for Course {CourseId}", lesson.Id, model.CourseId);
            TempData["Success"] = "تم إنشاء الدرس بنجاح";
        }
        else
        {
            // Update existing lesson
            var lesson = await _context.Lessons.FindAsync(model.Id);
            if (lesson == null)
                return NotFound();

            lesson.TitleAr = model.TitleAr;
            lesson.ContentAr = model.ContentHtml;
            lesson.VideoUrl = model.VideoUrl;
            lesson.DurationMinutes = model.DurationMinutes;
            lesson.SortOrder = model.SortOrder;
            lesson.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Lesson updated: ID {Id} for Course {CourseId}", model.Id, model.CourseId);
            TempData["Success"] = "تم تحديث الدرس بنجاح";
        }

        return RedirectToAction(nameof(Lessons), new { id = model.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LessonDelete(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null)
            return NotFound();

        var courseId = lesson.CourseId;
        _context.Lessons.Remove(lesson);

        // Update course lesson count
        var course = await _context.Courses.FindAsync(courseId);
        if (course != null)
        {
            var remainingCount = await _context.Lessons.CountAsync(l => l.CourseId == courseId && l.Id != id);
            course.LessonCount = remainingCount;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lesson deleted: ID {Id} from Course {CourseId}", id, courseId);
        TempData["Success"] = "تم حذف الدرس بنجاح";
        return RedirectToAction(nameof(Lessons), new { id = courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderLessons(int courseId, string lessonOrder)
    {
        if (string.IsNullOrWhiteSpace(lessonOrder))
        {
            TempData["Error"] = "بيانات الترتيب غير صحيحة";
            return RedirectToAction(nameof(Lessons), new { id = courseId });
        }

        var ids = lessonOrder.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
            .Where(v => v > 0)
            .ToList();

        var lessons = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .ToListAsync();

        for (int i = 0; i < ids.Count; i++)
        {
            var lesson = lessons.FirstOrDefault(l => l.Id == ids[i]);
            if (lesson != null)
            {
                lesson.SortOrder = i + 1;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Lessons reordered for Course {CourseId}", courseId);
        TempData["Success"] = "تم إعادة ترتيب الدروس بنجاح";
        return RedirectToAction(nameof(Lessons), new { id = courseId });
    }

    // ══════════════════════════════════════════════════════════
    //  QUIZ BUILDER (per Lesson)
    // ══════════════════════════════════════════════════════════

    public async Task<IActionResult> Quiz(int lessonId)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.LessonId == lessonId);

        var viewModel = new QuizEditViewModel
        {
            QuizId = quiz?.Id ?? 0,
            LessonId = lessonId,
            LessonTitleAr = lesson.TitleAr,
            CourseId = lesson.CourseId,
            CourseTitleAr = lesson.Course.TitleAr,
            PassingScore = quiz?.PassingScore ?? 70,
            Questions = quiz?.Questions
                .OrderBy(q => q.Id)
                .Select(q => MapQuestionToViewModel(q))
                .ToList() ?? new List<QuestionEditViewModel>()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuizSave(int lessonId, int passingScore)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.LessonId == lessonId);

        if (quiz == null)
        {
            quiz = new Quiz
            {
                LessonId = lessonId,
                PassingScore = passingScore
            };
            _context.Quizzes.Add(quiz);
            _logger.LogInformation("Quiz created for Lesson {LessonId}", lessonId);
        }
        else
        {
            quiz.PassingScore = passingScore;
            quiz.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Quiz updated for Lesson {LessonId}", lessonId);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = "تم حفظ إعدادات الاختبار بنجاح";
        return RedirectToAction(nameof(Quiz), new { lessonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuestionSave(QuestionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى تعبئة جميع الحقول المطلوبة";
            return RedirectToAction(nameof(Quiz), new { lessonId = GetLessonIdForQuiz(model.QuizId).Result });
        }

        // Build JSON options array
        var options = new List<string> { model.Option1, model.Option2, model.Option3, model.Option4 };
        var optionsJson = JsonSerializer.Serialize(options);

        if (model.Id == 0)
        {
            // Ensure quiz exists
            var quiz = await _context.Quizzes.FindAsync(model.QuizId);
            if (quiz == null)
                return NotFound();

            var question = new QuizQuestion
            {
                QuizId = model.QuizId,
                QuestionAr = model.QuestionAr,
                Options = optionsJson,
                CorrectIndex = model.CorrectIndex
            };

            _context.QuizQuestions.Add(question);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Question created: ID {Id} for Quiz {QuizId}", question.Id, model.QuizId);
            TempData["Success"] = "تم إضافة السؤال بنجاح";
        }
        else
        {
            var question = await _context.QuizQuestions.FindAsync(model.Id);
            if (question == null)
                return NotFound();

            question.QuestionAr = model.QuestionAr;
            question.Options = optionsJson;
            question.CorrectIndex = model.CorrectIndex;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Question updated: ID {Id} for Quiz {QuizId}", model.Id, model.QuizId);
            TempData["Success"] = "تم تحديث السؤال بنجاح";
        }

        var lessonId = await GetLessonIdForQuiz(model.QuizId);
        return RedirectToAction(nameof(Quiz), new { lessonId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuestionDelete(int id)
    {
        var question = await _context.QuizQuestions
            .Include(q => q.Quiz)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
            return NotFound();

        var lessonId = question.Quiz.LessonId;
        _context.QuizQuestions.Remove(question);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Question deleted: ID {Id} from Quiz {QuizId}", id, question.QuizId);
        TempData["Success"] = "تم حذف السؤال بنجاح";
        return RedirectToAction(nameof(Quiz), new { lessonId });
    }

    // ══════════════════════════════════════════════════════════
    //  ENROLLMENT ANALYTICS
    // ══════════════════════════════════════════════════════════

    public async Task<IActionResult> Enrollments()
    {
        var totalEnrollments = await _context.Enrollments
            .AsNoTracking()
            .CountAsync();

        var completedEnrollments = await _context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.Progress >= 100);

        var averageProgress = totalEnrollments > 0
            ? await _context.Enrollments.AsNoTracking().AverageAsync(e => (double)e.Progress)
            : 0;

        var totalCourses = await _context.Courses.AsNoTracking().CountAsync();
        var publishedCourses = await _context.Courses.AsNoTracking().CountAsync(c => c.IsPublished);
        var totalCertificates = await _context.Certificates.AsNoTracking().CountAsync();

        var completionRate = totalEnrollments > 0
            ? Math.Round((double)completedEnrollments / totalEnrollments * 100, 1)
            : 0;

        // Per-course stats
        var courseStats = await _context.Courses
            .AsNoTracking()
            .OrderByDescending(c => c.EnrollmentCount)
            .Select(c => new CourseStatsViewModel
            {
                CourseId = c.Id,
                CourseTitleAr = c.TitleAr,
                EnrollmentCount = c.EnrollmentCount,
                CompletedCount = c.Enrollments.Count(e => e.Progress >= 100),
                CompletionRate = c.EnrollmentCount > 0
                    ? Math.Round((double)c.Enrollments.Count(e => e.Progress >= 100) / c.EnrollmentCount * 100, 1)
                    : 0,
                AverageProgress = c.Enrollments.Any()
                    ? Math.Round(c.Enrollments.Average(e => (double)e.Progress), 1)
                    : 0,
                CertificateCount = c.Certificates.Count()
            })
            .ToListAsync();

        // Recent enrollments
        var recentEnrollments = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(50)
            .Select(e => new EnrollmentItemViewModel
            {
                UserId = e.UserId,
                UserDisplayName = e.User.DisplayName,
                UserEmail = e.User.Email,
                CourseId = e.CourseId,
                CourseTitleAr = e.Course.TitleAr,
                Progress = e.Progress,
                EnrolledAt = e.EnrolledAt
            })
            .ToListAsync();

        var viewModel = new EnrollmentStatsViewModel
        {
            TotalEnrollments = totalEnrollments,
            CompletedEnrollments = completedEnrollments,
            CompletionRate = completionRate,
            AverageProgress = Math.Round(averageProgress, 1),
            TotalCourses = totalCourses,
            PublishedCourses = publishedCourses,
            TotalCertificates = totalCertificates,
            CourseStats = courseStats,
            RecentEnrollments = recentEnrollments
        };

        return View(viewModel);
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    private async Task<int> GetLessonIdForQuiz(int quizId)
    {
        var quiz = await _context.Quizzes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == quizId);
        return quiz?.LessonId ?? 0;
    }

    private static QuestionEditViewModel MapQuestionToViewModel(QuizQuestion q)
    {
        var options = new List<string>();
        try
        {
            options = JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>();
        }
        catch
        {
            // If JSON parsing fails, use empty options
        }

        return new QuestionEditViewModel
        {
            Id = q.Id,
            QuizId = q.QuizId,
            QuestionAr = q.QuestionAr,
            Option1 = options.Count > 0 ? options[0] : string.Empty,
            Option2 = options.Count > 1 ? options[1] : string.Empty,
            Option3 = options.Count > 2 ? options[2] : string.Empty,
            Option4 = options.Count > 3 ? options[3] : string.Empty,
            CorrectIndex = q.CorrectIndex
        };
    }
}
