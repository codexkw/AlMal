using System.Globalization;
using AlMal.Application.DTOs.Market;
using AlMal.Application.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.ExternalApis;

/// <summary>
/// Scrapes market data from Boursa Kuwait (boursakuwait.com.kw) using HtmlAgilityPack.
/// Selector paths are best-effort placeholders; inspect the live site and adjust as needed.
/// </summary>
public sealed class BoursakuwaitScraper : IMarketDataProvider
{
    private const string BaseUrl = "https://www.boursakuwait.com.kw";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BoursakuwaitScraper> _logger;

    public BoursakuwaitScraper(
        IHttpClientFactory httpClientFactory,
        ILogger<BoursakuwaitScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ------------------------------------------------------------------ //
    //  Stock Prices
    // ------------------------------------------------------------------ //

    public async Task<IReadOnlyList<StockPriceData>> ScrapeStockPricesAsync(CancellationToken ct)
    {
        // TODO: Verify the actual URL path for the all-stocks market page.
        const string url = $"{BaseUrl}/ar/market/market-summary";

        var html = await FetchHtmlAsync(url, ct);
        if (html is null)
            return Array.Empty<StockPriceData>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<StockPriceData>();

        // TODO: Verify table selector – inspect the live page for the correct XPath.
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'market-summary-table')]//tbody//tr");
        if (rows is null)
        {
            _logger.LogWarning("ScrapeStockPricesAsync: no rows found at {Url}. The selector may need updating.", url);
            return results;
        }

        foreach (var row in rows)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var cells = row.SelectNodes("td");
                if (cells is null || cells.Count < 7)
                    continue;

                // TODO: Verify column order by inspecting the live table.
                var symbol = cells[0].InnerText.Trim();
                var nameAr = cells[1].InnerText.Trim();
                var sectorNameAr = cells[2].InnerText.Trim();
                var lastPrice = ParseDecimal(cells[3].InnerText);
                var dayChange = ParseDecimal(cells[4].InnerText);
                var dayChangePct = ParseDecimal(cells[5].InnerText);
                var volume = ParseLong(cells[6].InnerText);
                var value = cells.Count > 7 ? ParseNullableDecimal(cells[7].InnerText) : null;

                results.Add(new StockPriceData(
                    Symbol: symbol,
                    NameAr: nameAr,
                    NameEn: null, // English name may not be on the Arabic page
                    SectorNameAr: sectorNameAr,
                    LastPrice: lastPrice,
                    DayChange: dayChange,
                    DayChangePercent: dayChangePct,
                    Volume: volume,
                    Value: value));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScrapeStockPricesAsync: failed to parse a row.");
            }
        }

        _logger.LogInformation("ScrapeStockPricesAsync: scraped {Count} stock prices.", results.Count);
        return results;
    }

    // ------------------------------------------------------------------ //
    //  Market Indices
    // ------------------------------------------------------------------ //

    public async Task<IReadOnlyList<MarketIndexData>> ScrapeMarketIndicesAsync(CancellationToken ct)
    {
        // TODO: Verify the actual URL path for market indices.
        const string url = $"{BaseUrl}/ar/market/market-summary";

        var html = await FetchHtmlAsync(url, ct);
        if (html is null)
            return Array.Empty<MarketIndexData>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<MarketIndexData>();

        // TODO: Verify selector – look for the indices widget/section on the live page.
        var indexNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'market-indices')]//div[contains(@class,'index-item')]");
        if (indexNodes is null)
        {
            _logger.LogWarning("ScrapeMarketIndicesAsync: no index nodes found. The selector may need updating.");
            return results;
        }

        foreach (var node in indexNodes)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // TODO: Verify inner selectors for name, value, change, etc.
                var nameAr = node.SelectSingleNode(".//span[contains(@class,'index-name')]")?.InnerText.Trim() ?? "Unknown";
                var valueText = node.SelectSingleNode(".//span[contains(@class,'index-value')]")?.InnerText.Trim();
                var changeText = node.SelectSingleNode(".//span[contains(@class,'index-change')]")?.InnerText.Trim();
                var changePctText = node.SelectSingleNode(".//span[contains(@class,'index-change-pct')]")?.InnerText.Trim();

                var type = DetermineIndexType(nameAr);

                results.Add(new MarketIndexData(
                    NameAr: nameAr,
                    Type: type,
                    Value: ParseDecimal(valueText),
                    Change: ParseDecimal(changeText),
                    ChangePercent: ParseDecimal(changePctText)));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScrapeMarketIndicesAsync: failed to parse an index node.");
            }
        }

        _logger.LogInformation("ScrapeMarketIndicesAsync: scraped {Count} indices.", results.Count);
        return results;
    }

    // ------------------------------------------------------------------ //
    //  Stock Price History (OHLCV)
    // ------------------------------------------------------------------ //

    public async Task<IReadOnlyList<StockPriceHistoryData>> ScrapeStockPriceHistoryAsync(string symbol, CancellationToken ct)
    {
        // TODO: Verify the actual URL pattern for historical data.
        var url = $"{BaseUrl}/ar/stock/{symbol}/historical";

        var html = await FetchHtmlAsync(url, ct);
        if (html is null)
            return Array.Empty<StockPriceHistoryData>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<StockPriceHistoryData>();

        // TODO: Verify table selector for historical data.
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'historical-table')]//tbody//tr");
        if (rows is null)
        {
            _logger.LogWarning("ScrapeStockPriceHistoryAsync: no rows found for symbol {Symbol}. The selector may need updating.", symbol);
            return results;
        }

        foreach (var row in rows)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var cells = row.SelectNodes("td");
                if (cells is null || cells.Count < 6)
                    continue;

                // TODO: Verify column order – date, open, high, low, close, volume, value, trades.
                var dateText = cells[0].InnerText.Trim();
                if (!DateOnly.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                    continue;

                var open = ParseDecimal(cells[1].InnerText);
                var high = ParseDecimal(cells[2].InnerText);
                var low = ParseDecimal(cells[3].InnerText);
                var close = ParseDecimal(cells[4].InnerText);
                var volume = ParseLong(cells[5].InnerText);
                var value = cells.Count > 6 ? ParseNullableDecimal(cells[6].InnerText) : null;
                var trades = cells.Count > 7 ? ParseNullableInt(cells[7].InnerText) : null;

                results.Add(new StockPriceHistoryData(
                    Symbol: symbol,
                    Date: date,
                    Open: open,
                    High: high,
                    Low: low,
                    Close: close,
                    Volume: volume,
                    Value: value,
                    Trades: trades));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScrapeStockPriceHistoryAsync: failed to parse a row for symbol {Symbol}.", symbol);
            }
        }

        _logger.LogInformation("ScrapeStockPriceHistoryAsync: scraped {Count} history rows for {Symbol}.", results.Count, symbol);
        return results;
    }

    // ------------------------------------------------------------------ //
    //  Order Book
    // ------------------------------------------------------------------ //

    public async Task<IReadOnlyList<OrderBookData>> ScrapeOrderBookAsync(string symbol, CancellationToken ct)
    {
        // TODO: Verify the actual URL pattern for the order book.
        var url = $"{BaseUrl}/ar/stock/{symbol}/order-book";

        var html = await FetchHtmlAsync(url, ct);
        if (html is null)
            return Array.Empty<OrderBookData>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<OrderBookData>();

        // TODO: Verify table selector for order book.
        var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'order-book-table')]//tbody//tr");
        if (rows is null)
        {
            _logger.LogWarning("ScrapeOrderBookAsync: no rows found for symbol {Symbol}. The selector may need updating.", symbol);
            return results;
        }

        var level = 1;
        foreach (var row in rows)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var cells = row.SelectNodes("td");
                if (cells is null || cells.Count < 4)
                    continue;

                // TODO: Verify column order – bid qty, bid price, ask price, ask qty.
                var bidQty = ParseNullableLong(cells[0].InnerText);
                var bidPrice = ParseNullableDecimal(cells[1].InnerText);
                var askPrice = ParseNullableDecimal(cells[2].InnerText);
                var askQty = ParseNullableLong(cells[3].InnerText);

                results.Add(new OrderBookData(
                    Symbol: symbol,
                    Level: level++,
                    BidPrice: bidPrice,
                    BidQuantity: bidQty,
                    AskPrice: askPrice,
                    AskQuantity: askQty));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScrapeOrderBookAsync: failed to parse a row for symbol {Symbol}.", symbol);
            }
        }

        _logger.LogInformation("ScrapeOrderBookAsync: scraped {Count} order book levels for {Symbol}.", results.Count, symbol);
        return results;
    }

    // ------------------------------------------------------------------ //
    //  Disclosures
    // ------------------------------------------------------------------ //

    public async Task<IReadOnlyList<DisclosureData>> ScrapeDisclosuresAsync(CancellationToken ct)
    {
        // TODO: Verify the actual URL path for disclosures / announcements.
        const string url = $"{BaseUrl}/ar/disclosures";

        var html = await FetchHtmlAsync(url, ct);
        if (html is null)
            return Array.Empty<DisclosureData>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<DisclosureData>();

        // TODO: Verify selector – look for the disclosures list/table on the live page.
        var items = doc.DocumentNode.SelectNodes("//div[contains(@class,'disclosure-list')]//div[contains(@class,'disclosure-item')]");
        if (items is null)
        {
            _logger.LogWarning("ScrapeDisclosuresAsync: no disclosure nodes found. The selector may need updating.");
            return results;
        }

        foreach (var item in items)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // TODO: Verify inner selectors for disclosure fields.
                var stockSymbol = item.SelectSingleNode(".//span[contains(@class,'disclosure-symbol')]")?.InnerText.Trim() ?? "UNKNOWN";
                var titleAr = item.SelectSingleNode(".//a[contains(@class,'disclosure-title')]")?.InnerText.Trim() ?? string.Empty;
                var contentAr = item.SelectSingleNode(".//div[contains(@class,'disclosure-content')]")?.InnerText.Trim();
                var typeText = item.SelectSingleNode(".//span[contains(@class,'disclosure-type')]")?.InnerText.Trim() ?? "General";
                var dateText = item.SelectSingleNode(".//span[contains(@class,'disclosure-date')]")?.InnerText.Trim();
                var sourceUrl = item.SelectSingleNode(".//a[contains(@class,'disclosure-title')]")?.GetAttributeValue("href", string.Empty);

                var publishedDate = DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt
                    : DateTime.UtcNow;

                var type = ClassifyDisclosureType(typeText);

                if (!string.IsNullOrEmpty(sourceUrl) && !sourceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    sourceUrl = BaseUrl + sourceUrl;

                results.Add(new DisclosureData(
                    StockSymbol: stockSymbol,
                    TitleAr: titleAr,
                    ContentAr: contentAr,
                    Type: type,
                    PublishedDate: publishedDate,
                    SourceUrl: sourceUrl));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ScrapeDisclosuresAsync: failed to parse a disclosure item.");
            }
        }

        _logger.LogInformation("ScrapeDisclosuresAsync: scraped {Count} disclosures.", results.Count);
        return results;
    }

    // ================================================================== //
    //  Private Helpers
    // ================================================================== //

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken ct)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient("BoursakuwaitScraper");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch HTML from {Url}.", url);
            return null;
        }
    }

    private static decimal ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0m;

        text = text.Replace(",", "").Replace("%", "").Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0m;
    }

    private static decimal? ParseNullableDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Replace(",", "").Replace("%", "").Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : null;
    }

    private static long ParseLong(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Replace(",", "").Trim();
        return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    private static long? ParseNullableLong(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Replace(",", "").Trim();
        return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : null;
    }

    private static int? ParseNullableInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Replace(",", "").Trim();
        return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : null;
    }

    /// <summary>
    /// Determines the index type based on the Arabic name.
    /// </summary>
    private static string DetermineIndexType(string nameAr)
    {
        // TODO: Adjust these keywords after inspecting actual index names.
        if (nameAr.Contains("رئيسي", StringComparison.OrdinalIgnoreCase) || nameAr.Contains("عام", StringComparison.OrdinalIgnoreCase))
            return "Main";
        if (nameAr.Contains("أول", StringComparison.OrdinalIgnoreCase) || nameAr.Contains("Premier", StringComparison.OrdinalIgnoreCase))
            return "Premier";
        return "Sector";
    }

    /// <summary>
    /// Classifies a disclosure type from its Arabic text.
    /// </summary>
    private static string ClassifyDisclosureType(string typeText)
    {
        // TODO: Adjust keywords after inspecting actual disclosure type labels.
        if (typeText.Contains("مالي", StringComparison.OrdinalIgnoreCase) || typeText.Contains("Financial", StringComparison.OrdinalIgnoreCase))
            return "Financial";
        if (typeText.Contains("مجلس", StringComparison.OrdinalIgnoreCase) || typeText.Contains("Board", StringComparison.OrdinalIgnoreCase))
            return "Board";
        if (typeText.Contains("عمومية", StringComparison.OrdinalIgnoreCase) || typeText.Contains("AGM", StringComparison.OrdinalIgnoreCase))
            return "AGM";
        if (typeText.Contains("أرباح", StringComparison.OrdinalIgnoreCase) || typeText.Contains("توزيع", StringComparison.OrdinalIgnoreCase) || typeText.Contains("Dividend", StringComparison.OrdinalIgnoreCase))
            return "Dividend";
        return "General";
    }
}
