using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Portfolio;

// ── Portfolio Dashboard ──────────────────────────────────────────

public class PortfolioDashboardViewModel
{
    public int PortfolioId { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public bool IsPublic { get; set; }
    public List<HoldingViewModel> Holdings { get; set; } = new();
    public List<TradeHistoryViewModel> RecentTrades { get; set; } = new();
    public List<SectorAllocationViewModel> SectorAllocations { get; set; } = new();
    public List<PerformancePointViewModel> PerformanceHistory { get; set; } = new();
}

// ── Sector Allocation ────────────────────────────────────────────

public class SectorAllocationViewModel
{
    public string SectorNameAr { get; set; } = null!;
    public decimal MarketValue { get; set; }
    public decimal WeightPercent { get; set; }
}

// ── Performance Point ────────────────────────────────────────────

public class PerformancePointViewModel
{
    public string Date { get; set; } = null!;
    public decimal Value { get; set; }
}

// ── Holding ──────────────────────────────────────────────────────

public class HoldingViewModel
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

// ── Trade Form ───────────────────────────────────────────────────

public class TradeViewModel
{
    public int PortfolioId { get; set; }
    public decimal CashBalance { get; set; }
    public string TradeType { get; set; } = "Buy"; // "Buy" or "Sell"
    public int? StockId { get; set; }
    public string? StockSymbol { get; set; }
    public string? StockNameAr { get; set; }
    public decimal? StockPrice { get; set; }
    public int Quantity { get; set; }
    public int? MaxQuantity { get; set; } // For sell - max holding qty
    public List<HoldingViewModel> CurrentHoldings { get; set; } = new List<HoldingViewModel>();
}

// ── Trade History ────────────────────────────────────────────────

public class TradeHistoryViewModel
{
    public long Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public TradeType Type { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime ExecutedAt { get; set; }
}
