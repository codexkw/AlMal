namespace AlMal.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string QuestionAr { get; set; } = null!;
    public string Options { get; set; } = null!; // JSON array
    public int CorrectIndex { get; set; }

    // Navigation
    public Quiz Quiz { get; set; } = null!;
}
