namespace AlMal.Domain.Entities;

public class Quiz : BaseEntity
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public int PassingScore { get; set; } = 70;

    // Navigation
    public Lesson Lesson { get; set; } = null!;
    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
}
