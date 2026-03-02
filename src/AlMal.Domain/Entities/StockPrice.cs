namespace AlMal.Domain.Entities;

public class StockPrice : BaseEntity
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    public decimal? Value { get; set; }
    public int? Trades { get; set; }

    // Navigation
    public Stock Stock { get; set; } = null!;
}
