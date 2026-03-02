using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class Watchlist : BaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int StockId { get; set; }
    public decimal? AlertPrice { get; set; }
    public AlertType? AlertType { get; set; }
    public bool AlertEnabled { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Stock Stock { get; set; } = null!;
}
