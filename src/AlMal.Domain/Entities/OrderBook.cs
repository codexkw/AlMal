namespace AlMal.Domain.Entities;

public class OrderBook : BaseEntity
{
    public long Id { get; set; }
    public int StockId { get; set; }
    public int Level { get; set; }
    public decimal? BidPrice { get; set; }
    public long? BidQuantity { get; set; }
    public decimal? AskPrice { get; set; }
    public long? AskQuantity { get; set; }
    public DateTime Timestamp { get; set; }

    // Navigation
    public Stock Stock { get; set; } = null!;
}
