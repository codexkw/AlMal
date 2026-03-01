using AlMal.Application.DTOs.Market;

namespace AlMal.Application.Interfaces;

/// <summary>
/// Provides methods to scrape market data from an external source (e.g., Boursa Kuwait).
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// Scrapes current stock prices for all listed stocks.
    /// </summary>
    Task<IReadOnlyList<StockPriceData>> ScrapeStockPricesAsync(CancellationToken ct);

    /// <summary>
    /// Scrapes market index values (main, sector, premier).
    /// </summary>
    Task<IReadOnlyList<MarketIndexData>> ScrapeMarketIndicesAsync(CancellationToken ct);

    /// <summary>
    /// Scrapes OHLCV historical price data for a given stock symbol.
    /// </summary>
    Task<IReadOnlyList<StockPriceHistoryData>> ScrapeStockPriceHistoryAsync(string symbol, CancellationToken ct);

    /// <summary>
    /// Scrapes the order book (bid/ask levels) for a given stock symbol.
    /// </summary>
    Task<IReadOnlyList<OrderBookData>> ScrapeOrderBookAsync(string symbol, CancellationToken ct);

    /// <summary>
    /// Scrapes the latest company disclosures and announcements.
    /// </summary>
    Task<IReadOnlyList<DisclosureData>> ScrapeDisclosuresAsync(CancellationToken ct);
}
