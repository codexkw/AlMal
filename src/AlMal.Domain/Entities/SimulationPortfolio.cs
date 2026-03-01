namespace AlMal.Domain.Entities;

public class SimulationPortfolio : BaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public decimal InitialCapital { get; set; } = 100_000m;
    public decimal CashBalance { get; set; }
    public bool IsPublic { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public ICollection<SimulationTrade> Trades { get; set; } = new List<SimulationTrade>();
    public ICollection<SimulationHolding> Holdings { get; set; } = new List<SimulationHolding>();
}
