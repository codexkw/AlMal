namespace AlMal.Domain.Entities;

public class PostStockMention
{
    public long PostId { get; set; }
    public int StockId { get; set; }

    // Navigation
    public Post Post { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
