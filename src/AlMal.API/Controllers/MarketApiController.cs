using AlMal.Application.DTOs.Api;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.API.Controllers;

[ApiController]
[Route("api/v1/market")]
public class MarketApiController : ControllerBase
{
    private readonly AlMalDbContext _db;

    public MarketApiController(AlMalDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/v1/market/indices — All market indices
    /// </summary>
    [HttpGet("indices")]
    public async Task<IActionResult> GetIndices()
    {
        var indices = await _db.MarketIndices
            .AsNoTracking()
            .OrderBy(i => i.Type)
            .ThenBy(i => i.NameAr)
            .Select(i => new MarketIndexDto
            {
                Id = i.Id,
                NameAr = i.NameAr,
                Value = i.Value,
                Change = i.Change,
                ChangePercent = i.ChangePercent,
                Type = i.Type.ToString()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<MarketIndexDto>>.Ok(indices));
    }

    /// <summary>
    /// GET /api/v1/market/stocks — Paginated stock list with optional filtering and sorting
    /// </summary>
    [HttpGet("stocks")]
    public async Task<IActionResult> GetStocks(
        [FromQuery] int? sector,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive);

        // Filter by sector
        if (sector.HasValue)
        {
            query = query.Where(s => s.SectorId == sector.Value);
        }

        // Search by symbol or name
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Symbol.Contains(term) ||
                s.NameAr.Contains(term) ||
                (s.NameEn != null && s.NameEn.Contains(term)));
        }

        // Sorting
        query = sort?.ToLowerInvariant() switch
        {
            "symbol" => query.OrderBy(s => s.Symbol),
            "name" => query.OrderBy(s => s.NameAr),
            "price" => query.OrderByDescending(s => s.LastPrice),
            "change" => query.OrderByDescending(s => s.DayChangePercent),
            "volume" => query.OrderByDescending(s => s.MarketCap), // fallback: marketcap for "volume" when no Volume on Stock
            "marketcap" => query.OrderByDescending(s => s.MarketCap),
            _ => query.OrderBy(s => s.Symbol)
        };

        var totalCount = await query.CountAsync();

        var stocks = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StockDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap,
                Volume = s.StockPrices
                    .OrderByDescending(p => p.Date)
                    .Select(p => (long?)p.Volume)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<StockDto>>.Ok(stocks, pagination));
    }

    /// <summary>
    /// GET /api/v1/market/stocks/{symbol} — Single stock detail
    /// </summary>
    [HttpGet("stocks/{symbol}")]
    public async Task<IActionResult> GetStock(string symbol)
    {
        var stock = await _db.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.Symbol == symbol.ToUpperInvariant())
            .Select(s => new StockDetailDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap,
                SharesOutstanding = s.SharesOutstanding,
                ListingDate = s.ListingDate,
                DescriptionAr = s.DescriptionAr,
                LogoUrl = s.LogoUrl,
                Volume = s.StockPrices
                    .OrderByDescending(p => p.Date)
                    .Select(p => (long?)p.Volume)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (stock is null)
        {
            return NotFound(ApiResponse<StockDetailDto>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));
        }

        return Ok(ApiResponse<StockDetailDto>.Ok(stock));
    }

    /// <summary>
    /// GET /api/v1/market/stocks/{symbol}/prices — Price history (OHLCV)
    /// </summary>
    [HttpGet("stocks/{symbol}/prices")]
    public async Task<IActionResult> GetPriceHistory(
        string symbol,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var stockId = await _db.Stocks
            .AsNoTracking()
            .Where(s => s.Symbol == symbol.ToUpperInvariant())
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (stockId is null)
        {
            return NotFound(ApiResponse<List<PriceHistoryDto>>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));
        }

        var query = _db.StockPrices
            .AsNoTracking()
            .Where(p => p.StockId == stockId.Value);

        if (from.HasValue)
        {
            query = query.Where(p => p.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.Date <= to.Value);
        }

        var prices = await query
            .OrderBy(p => p.Date)
            .Select(p => new PriceHistoryDto
            {
                Time = p.Date.ToString("yyyy-MM-dd"),
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PriceHistoryDto>>.Ok(prices));
    }

    /// <summary>
    /// GET /api/v1/market/stocks/{symbol}/orderbook — Order book
    /// </summary>
    [HttpGet("stocks/{symbol}/orderbook")]
    public async Task<IActionResult> GetOrderBook(string symbol)
    {
        var stockId = await _db.Stocks
            .AsNoTracking()
            .Where(s => s.Symbol == symbol.ToUpperInvariant())
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (stockId is null)
        {
            return NotFound(ApiResponse<List<OrderBookEntryDto>>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));
        }

        // Get the most recent order book snapshot (by Timestamp)
        var latestTimestamp = await _db.OrderBooks
            .AsNoTracking()
            .Where(o => o.StockId == stockId.Value)
            .MaxAsync(o => (DateTime?)o.Timestamp);

        if (latestTimestamp is null)
        {
            return Ok(ApiResponse<List<OrderBookEntryDto>>.Ok(new List<OrderBookEntryDto>()));
        }

        var orderBook = await _db.OrderBooks
            .AsNoTracking()
            .Where(o => o.StockId == stockId.Value && o.Timestamp == latestTimestamp.Value)
            .OrderBy(o => o.Level)
            .Select(o => new OrderBookEntryDto
            {
                Level = o.Level,
                BidPrice = o.BidPrice,
                BidQuantity = o.BidQuantity,
                AskPrice = o.AskPrice,
                AskQuantity = o.AskQuantity
            })
            .ToListAsync();

        return Ok(ApiResponse<List<OrderBookEntryDto>>.Ok(orderBook));
    }

    /// <summary>
    /// GET /api/v1/market/stocks/{symbol}/financials — Financial ratios
    /// Calculates P/E, P/B, ROE, ProfitMargin, DividendYield, DebtEquity from latest FinancialStatement + Stock.LastPrice.
    /// </summary>
    [HttpGet("stocks/{symbol}/financials")]
    public async Task<IActionResult> GetFinancials(string symbol)
    {
        var stock = await _db.Stocks
            .AsNoTracking()
            .Where(s => s.Symbol == symbol.ToUpperInvariant())
            .Select(s => new { s.Id, s.LastPrice, s.SharesOutstanding })
            .FirstOrDefaultAsync();

        if (stock is null)
        {
            return NotFound(ApiResponse<FinancialRatiosDto>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));
        }

        var latestStatement = await _db.FinancialStatements
            .AsNoTracking()
            .Where(f => f.StockId == stock.Id)
            .OrderByDescending(f => f.Year)
            .ThenByDescending(f => f.Quarter)
            .FirstOrDefaultAsync();

        if (latestStatement is null)
        {
            return Ok(ApiResponse<FinancialRatiosDto>.Ok(new FinancialRatiosDto()));
        }

        var lastPrice = stock.LastPrice;
        var sharesOutstanding = stock.SharesOutstanding;

        // P/E = LastPrice / EPS (if EPS > 0)
        decimal? pe = null;
        if (latestStatement.EPS.HasValue && latestStatement.EPS.Value > 0 && lastPrice.HasValue)
        {
            pe = lastPrice.Value / latestStatement.EPS.Value;
        }

        // P/B = LastPrice * SharesOutstanding / TotalEquity (if equity > 0)
        decimal? pb = null;
        if (lastPrice.HasValue && sharesOutstanding.HasValue && latestStatement.TotalEquity.HasValue && latestStatement.TotalEquity.Value > 0)
        {
            pb = (lastPrice.Value * sharesOutstanding.Value) / latestStatement.TotalEquity.Value;
        }

        // ROE = NetIncome / TotalEquity * 100 (if equity > 0)
        decimal? roe = null;
        if (latestStatement.NetIncome.HasValue && latestStatement.TotalEquity.HasValue && latestStatement.TotalEquity.Value > 0)
        {
            roe = (latestStatement.NetIncome.Value / latestStatement.TotalEquity.Value) * 100;
        }

        // ProfitMargin = NetIncome / Revenue * 100 (if revenue > 0)
        decimal? profitMargin = null;
        if (latestStatement.NetIncome.HasValue && latestStatement.Revenue.HasValue && latestStatement.Revenue.Value > 0)
        {
            profitMargin = (latestStatement.NetIncome.Value / latestStatement.Revenue.Value) * 100;
        }

        // DividendYield = DPS / LastPrice * 100 (if price > 0)
        decimal? dividendYield = null;
        if (latestStatement.DPS.HasValue && lastPrice.HasValue && lastPrice.Value > 0)
        {
            dividendYield = (latestStatement.DPS.Value / lastPrice.Value) * 100;
        }

        // DebtEquity = TotalDebt / TotalEquity (if equity > 0)
        decimal? debtEquity = null;
        if (latestStatement.TotalDebt.HasValue && latestStatement.TotalEquity.HasValue && latestStatement.TotalEquity.Value > 0)
        {
            debtEquity = latestStatement.TotalDebt.Value / latestStatement.TotalEquity.Value;
        }

        var ratios = new FinancialRatiosDto
        {
            PE = pe,
            PB = pb,
            ROE = roe,
            ProfitMargin = profitMargin,
            DividendYield = dividendYield,
            DebtEquity = debtEquity,
            EPS = latestStatement.EPS,
            DPS = latestStatement.DPS,
            Year = latestStatement.Year,
            Quarter = latestStatement.Quarter
        };

        return Ok(ApiResponse<FinancialRatiosDto>.Ok(ratios));
    }

    /// <summary>
    /// GET /api/v1/market/stocks/{symbol}/disclosures — Paginated disclosures for a stock
    /// </summary>
    [HttpGet("stocks/{symbol}/disclosures")]
    public async Task<IActionResult> GetDisclosures(
        string symbol,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var stockId = await _db.Stocks
            .AsNoTracking()
            .Where(s => s.Symbol == symbol.ToUpperInvariant())
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();

        if (stockId is null)
        {
            return NotFound(ApiResponse<List<DisclosureDto>>.Fail("STOCK_NOT_FOUND", "السهم غير موجود"));
        }

        var query = _db.Disclosures
            .AsNoTracking()
            .Where(d => d.StockId == stockId.Value)
            .OrderByDescending(d => d.PublishedDate);

        var totalCount = await query.CountAsync();

        var disclosures = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DisclosureDto
            {
                Id = d.Id,
                TitleAr = d.TitleAr,
                Type = d.Type.ToString(),
                PublishedDate = d.PublishedDate,
                SourceUrl = d.SourceUrl,
                AiSummary = d.AiSummary
            })
            .ToListAsync();

        var pagination = new PaginationInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<List<DisclosureDto>>.Ok(disclosures, pagination));
    }

    /// <summary>
    /// GET /api/v1/market/gainers — Top 10 stocks by DayChangePercent descending
    /// </summary>
    [HttpGet("gainers")]
    public async Task<IActionResult> GetGainers()
    {
        var gainers = await _db.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent > 0)
            .OrderByDescending(s => s.DayChangePercent)
            .Take(10)
            .Select(s => new StockDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap,
                Volume = s.StockPrices
                    .OrderByDescending(p => p.Date)
                    .Select(p => (long?)p.Volume)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<StockDto>>.Ok(gainers));
    }

    /// <summary>
    /// GET /api/v1/market/losers — Top 10 stocks by DayChangePercent ascending
    /// </summary>
    [HttpGet("losers")]
    public async Task<IActionResult> GetLosers()
    {
        var losers = await _db.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive && s.DayChangePercent.HasValue && s.DayChangePercent < 0)
            .OrderBy(s => s.DayChangePercent)
            .Take(10)
            .Select(s => new StockDto
            {
                Id = s.Id,
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                SectorNameAr = s.Sector.NameAr,
                SectorId = s.SectorId,
                LastPrice = s.LastPrice,
                DayChange = s.DayChange,
                DayChangePercent = s.DayChangePercent,
                MarketCap = s.MarketCap,
                Volume = s.StockPrices
                    .OrderByDescending(p => p.Date)
                    .Select(p => (long?)p.Volume)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<StockDto>>.Ok(losers));
    }

    /// <summary>
    /// GET /api/v1/market/most-traded — Top 10 stocks by latest day's Volume descending
    /// </summary>
    [HttpGet("most-traded")]
    public async Task<IActionResult> GetMostTraded()
    {
        // Find the latest trading date across all stock prices
        var latestDate = await _db.StockPrices
            .AsNoTracking()
            .MaxAsync(p => (DateOnly?)p.Date);

        if (latestDate is null)
        {
            return Ok(ApiResponse<List<StockDto>>.Ok(new List<StockDto>()));
        }

        var mostTraded = await _db.StockPrices
            .AsNoTracking()
            .Where(p => p.Date == latestDate.Value)
            .OrderByDescending(p => p.Volume)
            .Take(10)
            .Select(p => new StockDto
            {
                Id = p.Stock.Id,
                Symbol = p.Stock.Symbol,
                NameAr = p.Stock.NameAr,
                NameEn = p.Stock.NameEn,
                SectorNameAr = p.Stock.Sector.NameAr,
                SectorId = p.Stock.SectorId,
                LastPrice = p.Stock.LastPrice,
                DayChange = p.Stock.DayChange,
                DayChangePercent = p.Stock.DayChangePercent,
                MarketCap = p.Stock.MarketCap,
                Volume = p.Volume
            })
            .ToListAsync();

        return Ok(ApiResponse<List<StockDto>>.Ok(mostTraded));
    }

    /// <summary>
    /// GET /api/v1/market/heatmap — All active stocks for heatmap visualization
    /// </summary>
    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap()
    {
        var items = await _db.Stocks
            .AsNoTracking()
            .Include(s => s.Sector)
            .Where(s => s.IsActive)
            .Select(s => new HeatmapItemDto
            {
                Symbol = s.Symbol,
                NameAr = s.NameAr,
                DayChangePercent = s.DayChangePercent,
                Volume = s.StockPrices
                    .OrderByDescending(p => p.Date)
                    .Select(p => (long?)p.Volume)
                    .FirstOrDefault(),
                MarketCap = s.MarketCap,
                SectorNameAr = s.Sector.NameAr
            })
            .ToListAsync();

        return Ok(ApiResponse<List<HeatmapItemDto>>.Ok(items));
    }

    /// <summary>
    /// GET /api/v1/market/sectors — All sectors with stock count
    /// </summary>
    [HttpGet("sectors")]
    public async Task<IActionResult> GetSectors()
    {
        var sectors = await _db.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorDto
            {
                Id = s.Id,
                NameAr = s.NameAr,
                NameEn = s.NameEn,
                StockCount = s.Stocks.Count(st => st.IsActive)
            })
            .ToListAsync();

        return Ok(ApiResponse<List<SectorDto>>.Ok(sectors));
    }
}
