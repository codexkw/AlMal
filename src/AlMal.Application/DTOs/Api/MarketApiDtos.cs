namespace AlMal.Application.DTOs.Api;

// Stock list item
public class StockDto
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string SectorNameAr { get; set; } = null!;
    public int SectorId { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public decimal? MarketCap { get; set; }
    public long? Volume { get; set; }
}

// Full stock detail
public class StockDetailDto : StockDto
{
    public long? SharesOutstanding { get; set; }
    public DateOnly? ListingDate { get; set; }
    public string? DescriptionAr { get; set; }
    public string? LogoUrl { get; set; }
}

// Market index
public class MarketIndexDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public decimal Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public string Type { get; set; } = null!;
}

// Price history (OHLCV)
public class PriceHistoryDto
{
    public string Time { get; set; } = null!; // yyyy-MM-dd
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

// Order book entry
public class OrderBookEntryDto
{
    public int Level { get; set; }
    public decimal? BidPrice { get; set; }
    public long? BidQuantity { get; set; }
    public decimal? AskPrice { get; set; }
    public long? AskQuantity { get; set; }
}

// Financial ratios
public class FinancialRatiosDto
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

// Disclosure
public class DisclosureDto
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTime PublishedDate { get; set; }
    public string? SourceUrl { get; set; }
    public string? AiSummary { get; set; }
}

// Sector
public class SectorDto
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public int StockCount { get; set; }
}

// Heatmap item
public class HeatmapItemDto
{
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public decimal? DayChangePercent { get; set; }
    public long? Volume { get; set; }
    public decimal? MarketCap { get; set; }
    public string SectorNameAr { get; set; } = null!;
}
