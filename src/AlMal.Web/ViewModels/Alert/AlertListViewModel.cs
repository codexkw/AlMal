using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Alert;

public class AlertListViewModel
{
    public List<AlertItemViewModel> Alerts { get; set; } = new List<AlertItemViewModel>();
}

public class AlertItemViewModel
{
    public int Id { get; set; }
    public AlertType Type { get; set; }
    public string TypeDisplay => Type switch
    {
        AlertType.Price => "تنبيه سعر",
        AlertType.Volume => "تنبيه حجم",
        AlertType.Disclosure => "إفصاح جديد",
        AlertType.Index => "تحرك المؤشر",
        _ => "تنبيه"
    };
    public string TypeIcon => Type switch
    {
        AlertType.Price => "bi-currency-exchange",
        AlertType.Volume => "bi-bar-chart-fill",
        AlertType.Disclosure => "bi-file-earmark-text",
        AlertType.Index => "bi-graph-up-arrow",
        _ => "bi-bell"
    };
    public string? StockSymbol { get; set; }
    public string? StockNameAr { get; set; }
    public string Condition { get; set; } = null!;
    public string ConditionDisplay => Condition?.ToLowerInvariant() switch
    {
        "above" => "أعلى من",
        "below" => "أقل من",
        _ => Condition ?? ""
    };
    public decimal? TargetValue { get; set; }
    public AlertChannel Channel { get; set; }
    public string ChannelDisplay => Channel switch
    {
        AlertChannel.App => "التطبيق",
        AlertChannel.WhatsApp => "واتساب",
        AlertChannel.Both => "التطبيق + واتساب",
        _ => "التطبيق"
    };
    public bool IsActive { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
}
