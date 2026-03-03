using System.Text.Json;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Infrastructure.Services;

public class CourseService : ICourseService
{
    private readonly AlMalDbContext _context;

    public CourseService(AlMalDbContext context)
    {
        _context = context;
    }

    public async Task<CourseCatalogResult> GetCourseCatalogAsync(
        string? difficulty, int page, int pageSize, string? userId, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var query = _context.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished);

        if (!string.IsNullOrEmpty(difficulty))
        {
            query = difficulty.ToLower() switch
            {
                "free" => query.Where(c => c.IsFree),
                "paid" => query.Where(c => !c.IsFree),
                _ => query
            };
        }

        var totalCount = await query.CountAsync(ct);

        var courses = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseSummary
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
            .ToListAsync(ct);

        if (userId != null && courses.Count > 0)
        {
            var courseIds = courses.Select(c => c.Id).ToList();
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.UserId == userId && courseIds.Contains(e.CourseId))
                .Select(e => new { e.CourseId, e.Progress })
                .ToListAsync(ct);

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

        return new CourseCatalogResult
        {
            Courses = courses,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CourseDetailResult?> GetCourseDetailAsync(int courseId, string? userId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId && c.IsPublished)
            .Select(c => new CourseDetailResult
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
                    .Select(l => new LessonSummary
                    {
                        Id = l.Id,
                        TitleAr = l.TitleAr,
                        SortOrder = l.SortOrder,
                        DurationMinutes = l.DurationMinutes,
                        HasQuiz = l.Quiz != null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (course == null)
            return null;

        if (userId != null)
        {
            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

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

        return course;
    }

    public async Task<LessonDetailResult?> GetLessonAsync(int lessonId, string userId, CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Quiz)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null)
            return null;

        var enrollment = await _context.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId, ct);

        if (enrollment == null)
            return null; // Not enrolled

        var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

        var allLessons = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == lesson.CourseId)
            .OrderBy(l => l.SortOrder)
            .Select(l => l.Id)
            .ToListAsync(ct);

        var currentIndex = allLessons.IndexOf(lessonId);

        return new LessonDetailResult
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
    }

    public async Task<EnrollResult> EnrollAsync(int courseId, string userId, CancellationToken ct = default)
    {
        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished, ct);

        if (course == null)
            return new EnrollResult { Success = false, CourseId = courseId, Message = "الدورة غير موجودة" };

        var existing = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

        if (existing != null)
            return new EnrollResult { Success = true, AlreadyEnrolled = true, CourseId = courseId, Message = "أنت مسجل بالفعل في هذه الدورة" };

        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            Progress = 0,
            CompletedLessonIds = "[]",
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);

        var courseEntity = await _context.Courses.FindAsync(new object[] { courseId }, ct);
        if (courseEntity != null)
        {
            courseEntity.EnrollmentCount++;
        }

        await _context.SaveChangesAsync(ct);

        return new EnrollResult { Success = true, CourseId = courseId, Message = "تم التسجيل بنجاح" };
    }

    public async Task<CompleteLessonResult> CompleteLessonAsync(int lessonId, string userId, CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null)
            return new CompleteLessonResult { Success = false };

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId, ct);

        if (enrollment == null)
            return new CompleteLessonResult { Success = false, CourseId = lesson.CourseId };

        var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

        if (!completedIds.Contains(lessonId))
        {
            completedIds.Add(lessonId);
            enrollment.CompletedLessonIds = JsonSerializer.Serialize(completedIds);

            var totalLessons = await _context.Lessons
                .AsNoTracking()
                .CountAsync(l => l.CourseId == lesson.CourseId, ct);

            enrollment.Progress = totalLessons > 0
                ? (int)Math.Round(completedIds.Count * 100.0 / totalLessons)
                : 0;

            await _context.SaveChangesAsync(ct);
        }

        var nextLesson = await _context.Lessons
            .AsNoTracking()
            .Where(l => l.CourseId == lesson.CourseId && l.SortOrder > lesson.SortOrder)
            .OrderBy(l => l.SortOrder)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(ct);

        return new CompleteLessonResult
        {
            Success = true,
            NextLessonId = nextLesson,
            CourseId = lesson.CourseId
        };
    }

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
}
