using AlMal.Domain.Enums;

namespace AlMal.Application.DTOs.Api;

public class AlertDto
{
    public int Id { get; set; }
    public string Type { get; set; } = null!;
    public int? StockId { get; set; }
    public string? StockSymbol { get; set; }
    public string? StockNameAr { get; set; }
    public string Condition { get; set; } = null!;
    public decimal? TargetValue { get; set; }
    public string Channel { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAlertDto
{
    public AlertType Type { get; set; }
    public int? StockId { get; set; }
    public string Condition { get; set; } = "above";
    public decimal? TargetValue { get; set; }
    public AlertChannel Channel { get; set; } = AlertChannel.Both;
}

public class NotificationDto
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
