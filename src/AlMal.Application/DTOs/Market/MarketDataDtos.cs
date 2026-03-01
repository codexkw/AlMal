namespace AlMal.Application.DTOs.Market;

/// <summary>
/// Represents a single stock's current price snapshot.
/// </summary>
public record StockPriceData(
    string Symbol,
    string NameAr,
    string? NameEn,
    string SectorNameAr,
    decimal LastPrice,
    decimal DayChange,
    decimal DayChangePercent,
    long Volume,
    decimal? Value);

/// <summary>
/// Represents a market index value (Main, Sector, or Premier).
/// </summary>
public record MarketIndexData(
    string NameAr,
    string Type, // "Main", "Sector", "Premier"
    decimal Value,
    decimal Change,
    decimal ChangePercent);

/// <summary>
/// Represents one day of OHLCV historical price data for a stock.
/// </summary>
public record StockPriceHistoryData(
    string Symbol,
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    decimal? Value,
    int? Trades);

/// <summary>
/// Represents a single level in the order book for a stock.
/// </summary>
public record OrderBookData(
    string Symbol,
    int Level,
    decimal? BidPrice,
    long? BidQuantity,
    decimal? AskPrice,
    long? AskQuantity);

/// <summary>
/// Represents a company disclosure or announcement.
/// </summary>
public record DisclosureData(
    string StockSymbol,
    string TitleAr,
    string? ContentAr,
    string Type, // Financial, Board, General, AGM, Dividend
    DateTime PublishedDate,
    string? SourceUrl);
