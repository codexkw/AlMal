using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class SimulationTrade : BaseEntity
{
    public long Id { get; set; }
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public TradeType Type { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime ExecutedAt { get; set; }

    // Navigation
    public SimulationPortfolio Portfolio { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
