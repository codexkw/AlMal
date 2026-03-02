using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class NewsArticle : BaseEntity
{
    public long Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public Sentiment Sentiment { get; set; }
    public string? Summary { get; set; }
    public string? ContextData { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? ExternalId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsProcessed { get; set; }

    // Navigation
    public ICollection<NewsArticleStock> NewsArticleStocks { get; set; } = new List<NewsArticleStock>();
}
