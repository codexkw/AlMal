using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.News;

public class NewsFeedViewModel
{
    public List<NewsCardViewModel> Articles { get; set; } = [];
    public List<SectorFilterOption> Sectors { get; set; } = [];
    public string? StockFilter { get; set; }
    public int? SectorFilter { get; set; }
    public string? SentimentFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
}

public class NewsCardViewModel
{
    public long Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public Sentiment Sentiment { get; set; }
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsProcessed { get; set; }
    public List<RelatedStockTag> RelatedStocks { get; set; } = [];
}

public class RelatedStockTag
{
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
}

public class SectorFilterOption
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
}

public class NewsDetailViewModel
{
    public long Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string? SourceUrl { get; set; }
    public Sentiment Sentiment { get; set; }
    public string? Summary { get; set; }
    public string? ContextData { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool IsProcessed { get; set; }
    public List<RelatedStockTag> RelatedStocks { get; set; } = [];
}
