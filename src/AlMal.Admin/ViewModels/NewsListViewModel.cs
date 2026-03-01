using AlMal.Domain.Enums;

namespace AlMal.Admin.ViewModels;

public class NewsListViewModel
{
    public List<NewsListItemViewModel> Items { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? SourceFilter { get; set; }
    public string? SentimentFilter { get; set; }
    public bool? ProcessedFilter { get; set; }
    public List<string> AvailableSources { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class NewsListItemViewModel
{
    public long Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Source { get; set; } = null!;
    public Sentiment Sentiment { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime PublishedAt { get; set; }
    public bool HasSummary { get; set; }

    public string TruncatedTitle =>
        TitleAr.Length > 100 ? TitleAr[..100] + "..." : TitleAr;
}
