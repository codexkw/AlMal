using System.Text.Json;
using AlMal.Application.Interfaces;
using AlMal.Domain.Entities;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Infrastructure.Services;

public class QuizService : IQuizService
{
    private readonly AlMalDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuizService(AlMalDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<QuizDetail?> GetQuizForLessonAsync(int lessonId, string userId, CancellationToken ct = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct);

        if (lesson == null)
            return null;

        var isEnrolled = await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && e.CourseId == lesson.CourseId, ct);

        if (!isEnrolled)
            return null;

        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.LessonId == lessonId, ct);

        if (quiz == null)
            return null;

        return new QuizDetail
        {
            QuizId = quiz.Id,
            LessonId = lessonId,
            LessonTitleAr = lesson.TitleAr,
            CourseTitleAr = lesson.Course.TitleAr,
            CourseId = lesson.CourseId,
            PassPercentage = quiz.PassingScore,
            Questions = quiz.Questions
                .Select(q => new QuizQuestionDetail
                {
                    Id = q.Id,
                    QuestionAr = q.QuestionAr,
                    Options = DeserializeOptions(q.Options),
                    CorrectIndex = q.CorrectIndex
                })
                .ToList()
        };
    }

    public async Task<QuizSubmissionResult?> SubmitQuizAsync(
        int quizId, Dictionary<int, int> answers, string userId, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .Include(q => q.Lesson)
                .ThenInclude(l => l.Course)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        if (quiz == null)
            return null;

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == quiz.Lesson.CourseId, ct);

        if (enrollment == null)
            return null;

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

        var result = new QuizSubmissionResult
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
            var completedIds = ParseCompletedLessonIds(enrollment.CompletedLessonIds);

            if (!completedIds.Contains(quiz.LessonId))
            {
                completedIds.Add(quiz.LessonId);
                enrollment.CompletedLessonIds = JsonSerializer.Serialize(completedIds);
            }

            var allLessonIds = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == quiz.Lesson.CourseId)
                .Select(l => l.Id)
                .ToListAsync(ct);

            int totalLessons = allLessonIds.Count;
            enrollment.Progress = totalLessons > 0
                ? (int)Math.Round(completedIds.Count * 100.0 / totalLessons)
                : 0;

            bool courseCompleted = allLessonIds.All(lid => completedIds.Contains(lid));

            if (courseCompleted)
            {
                enrollment.Progress = 100;
                result.CourseCompleted = true;

                var existingCertificate = await _context.Certificates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == quiz.Lesson.CourseId, ct);

                if (existingCertificate == null)
                {
                    var certificate = new Certificate
                    {
                        UserId = userId,
                        CourseId = quiz.Lesson.CourseId,
                        CertificateNumber = $"ALMAL-{quiz.Lesson.CourseId:D4}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        IssuedAt = DateTime.UtcNow
                    };

                    _context.Certificates.Add(certificate);
                    await _context.SaveChangesAsync(ct);

                    result.CertificateId = certificate.Id;

                    if (!quiz.Lesson.Course.IsFree)
                    {
                        var user = await _userManager.FindByIdAsync(userId);
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

            await _context.SaveChangesAsync(ct);
        }

        return result;
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
