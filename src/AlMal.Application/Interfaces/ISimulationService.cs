using AlMal.Domain.Entities;
using AlMal.Domain.Enums;

namespace AlMal.Application.Interfaces;

public interface ISimulationService
{
    Task<SimulationPortfolio> GetOrCreatePortfolioAsync(string userId, CancellationToken ct = default);
    Task<PortfolioSummary> GetPortfolioSummaryAsync(string userId, CancellationToken ct = default);
    Task<TradeResult> ExecuteTradeAsync(string userId, int stockId, int quantity, TradeType type, CancellationToken ct = default);
    Task ResetPortfolioAsync(string userId, CancellationToken ct = default);
    Task TogglePublicAsync(string userId, CancellationToken ct = default);
    Task<PortfolioSummary?> GetPublicPortfolioAsync(string userId, CancellationToken ct = default);
    Task<List<SectorAllocation>> GetSectorAllocationsAsync(int portfolioId, CancellationToken ct = default);
    Task<List<PerformancePoint>> GetPerformanceHistoryAsync(int portfolioId, CancellationToken ct = default);
}

public class PortfolioSummary
{
    public int PortfolioId { get; set; }
    public decimal InitialCapital { get; set; }
    public decimal CashBalance { get; set; }
    public decimal TotalPortfolioValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public bool IsPublic { get; set; }
    public string? OwnerDisplayName { get; set; }
    public List<HoldingSummary> Holdings { get; set; } = new();
    public List<TradeSummary> RecentTrades { get; set; } = new();
}

public class HoldingSummary
{
    public int StockId { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? SectorNameAr { get; set; }
    public int SectorId { get; set; }
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal PnL { get; set; }
    public decimal PnLPercent { get; set; }
    public decimal WeightPercent { get; set; }
}

public class TradeSummary
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

public class SectorAllocation
{
    public int SectorId { get; set; }
    public string SectorNameAr { get; set; } = null!;
    public decimal MarketValue { get; set; }
    public decimal WeightPercent { get; set; }
}

public class PerformancePoint
{
    public DateTime Date { get; set; }
    public decimal PortfolioValue { get; set; }
}

public class TradeResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static TradeResult Ok() => new() { Success = true };
    public static TradeResult Fail(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
