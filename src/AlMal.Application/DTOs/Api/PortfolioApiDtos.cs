namespace AlMal.Application.DTOs.Api;

// ── Portfolio DTO ────────────────────────────────────────────────

public class PortfolioDto
{
    public int Id { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public bool IsPublic { get; set; }
    public List<HoldingDto> Holdings { get; set; } = new List<HoldingDto>();
    public List<TradeDto> RecentTrades { get; set; } = new List<TradeDto>();
}

// ── Holding DTO ──────────────────────────────────────────────────

public class HoldingDto
{
    public int StockId { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public decimal WeightPercent { get; set; }
}

// ── Trade DTO ────────────────────────────────────────────────────

public class TradeDto
{
    public long Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime ExecutedAt { get; set; }
}

// ── Performance DTO ──────────────────────────────────────────────

public class PerformanceDto
{
    public decimal InitialCapital { get; set; }
    public decimal CashBalance { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public int TotalTrades { get; set; }
    public int BuyTrades { get; set; }
    public int SellTrades { get; set; }
    public int UniqueStocksTraded { get; set; }
    public int CurrentHoldingsCount { get; set; }
}

// ── Execute Trade (Input) DTO ────────────────────────────────────

public class ExecuteTradeDto
{
    public string Type { get; set; } = null!; // "Buy" or "Sell"
    public int StockId { get; set; }
    public int Quantity { get; set; }
}

// ── Sector Allocation DTO ───────────────────────────────────────

public class SectorAllocationApiDto
{
    public string SectorNameAr { get; set; } = null!;
    public decimal MarketValue { get; set; }
    public decimal WeightPercent { get; set; }
}
