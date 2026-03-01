namespace AlMal.Domain.Entities;

public class SimulationHolding
{
    public int PortfolioId { get; set; }
    public int StockId { get; set; }
    public int Quantity { get; set; }
    public decimal AverageCost { get; set; }

    // Navigation
    public SimulationPortfolio Portfolio { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
