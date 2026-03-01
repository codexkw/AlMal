using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Stock;

public class StockDetailViewModel
{
    // Header
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string SectorNameAr { get; set; } = null!;
    public int SectorId { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public bool IsInWatchlist { get; set; }

    // Tab A: Overview
    public string? DescriptionAr { get; set; }
    public DateOnly? ListingDate { get; set; }
    public decimal? MarketCap { get; set; }
    public long? SharesOutstanding { get; set; }

    // Tab B: Chart data endpoint
    // (loaded via AJAX from /api/v1/market/stocks/{symbol}/prices)

    // Tab D: Financial Ratios
    public FinancialRatiosViewModel? Financials { get; set; }

    // Tab E: Disclosures
    public List<DisclosureViewModel> RecentDisclosures { get; set; } = [];

    // Tab F: Order Book
    public List<OrderBookEntryViewModel> OrderBook { get; set; } = [];
    public decimal BuyPressure { get; set; }
    public decimal SellPressure { get; set; }
}

public class FinancialRatiosViewModel
{
    public decimal? PE { get; set; }
    public decimal? PB { get; set; }
    public decimal? ROE { get; set; }
    public decimal? ProfitMargin { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? DebtEquity { get; set; }
    public decimal? EPS { get; set; }
    public decimal? DPS { get; set; }
    public int? Year { get; set; }
    public int? Quarter { get; set; }
}

public class DisclosureViewModel
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? AiSummary { get; set; }
    public DisclosureType Type { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? SourceUrl { get; set; }
}

public class OrderBookEntryViewModel
{
    public int Level { get; set; }
    public decimal? BidPrice { get; set; }
    public long? BidQuantity { get; set; }
    public decimal? AskPrice { get; set; }
    public long? AskQuantity { get; set; }
}
