using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class AlertHistory : BaseEntity
{
    public long Id { get; set; }
    public int AlertId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Message { get; set; } = null!;
    public DeliveryStatus DeliveryStatus { get; set; }
    public AlertChannel Channel { get; set; }

    // Navigation
    public Alert Alert { get; set; } = null!;
}
