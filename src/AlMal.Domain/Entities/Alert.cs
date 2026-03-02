using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class Alert : BaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public AlertType Type { get; set; }
    public int? StockId { get; set; }
    public string Condition { get; set; } = null!;
    public decimal? TargetValue { get; set; }
    public AlertChannel Channel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggered { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Stock? Stock { get; set; }
    public ICollection<AlertHistory> AlertHistories { get; set; } = new List<AlertHistory>();
}
