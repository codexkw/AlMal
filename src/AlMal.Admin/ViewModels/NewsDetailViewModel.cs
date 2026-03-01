using AlMal.Domain.Enums;

namespace AlMal.Admin.ViewModels;

public class NewsDetailViewModel
{
    public long Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public Sentiment Sentiment { get; set; }
    public string? Summary { get; set; }
    public string? ContextData { get; set; }
    public DateTime PublishedAt { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsProcessed { get; set; }
    public List<RelatedStockViewModel> RelatedStocks { get; set; } = [];
}

public class RelatedStockViewModel
{
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
}
