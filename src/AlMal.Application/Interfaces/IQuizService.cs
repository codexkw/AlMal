namespace AlMal.Application.Interfaces;

public class QuizDetail
{
    public int QuizId { get; set; }
    public int LessonId { get; set; }
    public string LessonTitleAr { get; set; } = null!;
    public string CourseTitleAr { get; set; } = null!;
    public int CourseId { get; set; }
    public int PassPercentage { get; set; }
    public List<QuizQuestionDetail> Questions { get; set; } = [];
}

public class QuizQuestionDetail
{
    public int Id { get; set; }
    public string QuestionAr { get; set; } = null!;
    public List<string> Options { get; set; } = [];
    public int CorrectIndex { get; set; }
}

public class QuizSubmissionResult
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

public interface IQuizService
{
    Task<QuizDetail?> GetQuizForLessonAsync(int lessonId, string userId, CancellationToken ct = default);
    Task<QuizSubmissionResult?> SubmitQuizAsync(int quizId, Dictionary<int, int> answers, string userId, CancellationToken ct = default);
}
