namespace AlMal.Domain.Entities;

public class Stock : BaseEntity
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public int SectorId { get; set; }
    public string? ISIN { get; set; }
    public DateOnly? ListingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal? MarketCap { get; set; }
    public long? SharesOutstanding { get; set; }
    public string? LogoUrl { get; set; }
    public string? DescriptionAr { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }

    // Navigation
    public Sector Sector { get; set; } = null!;
    public ICollection<StockPrice> StockPrices { get; set; } = new List<StockPrice>();
    public ICollection<OrderBook> OrderBooks { get; set; } = new List<OrderBook>();
    public ICollection<FinancialStatement> FinancialStatements { get; set; } = new List<FinancialStatement>();
    public ICollection<Disclosure> Disclosures { get; set; } = new List<Disclosure>();
    public ICollection<Watchlist> Watchlists { get; set; } = new List<Watchlist>();
    public ICollection<PostStockMention> PostStockMentions { get; set; } = new List<PostStockMention>();
    public ICollection<NewsArticleStock> NewsArticleStocks { get; set; } = new List<NewsArticleStock>();
}
