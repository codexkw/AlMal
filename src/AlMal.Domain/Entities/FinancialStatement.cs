namespace AlMal.Domain.Entities;

public class FinancialStatement : BaseEntity
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal? Revenue { get; set; }
    public decimal? NetIncome { get; set; }
    public decimal? TotalAssets { get; set; }
    public decimal? TotalEquity { get; set; }
    public decimal? TotalDebt { get; set; }
    public decimal? EPS { get; set; }
    public decimal? DPS { get; set; }
    public decimal? BookValue { get; set; }

    // Navigation
    public Stock Stock { get; set; } = null!;
}
