namespace AlMal.Domain.Entities;

public class NewsArticleStock
{
    public long NewsArticleId { get; set; }
    public int StockId { get; set; }

    // Navigation
    public NewsArticle NewsArticle { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
