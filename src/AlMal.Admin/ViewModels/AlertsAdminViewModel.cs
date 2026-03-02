using AlMal.Domain.Enums;

namespace AlMal.Admin.ViewModels;

/// <summary>
/// ViewModel for the admin alerts list page
/// </summary>
public class AlertsAdminViewModel
{
    public List<AlertItemViewModel> Alerts { get; set; } = new List<AlertItemViewModel>();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    // Filters
    public AlertType? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }

    // Delivery Stats
    public AlertDeliveryStatsViewModel DeliveryStats { get; set; } = new AlertDeliveryStatsViewModel();
}

public class AlertItemViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string? UserEmail { get; set; }
    public AlertType Type { get; set; }
    public string? StockSymbol { get; set; }
    public string? StockNameAr { get; set; }
    public string Condition { get; set; } = null!;
    public decimal? TargetValue { get; set; }
    public AlertChannel Channel { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TriggerCount { get; set; }

    public string TypeArabic => Type switch
    {
        AlertType.Price => "سعر",
        AlertType.Disclosure => "إفصاح",
        AlertType.Volume => "حجم تداول",
        AlertType.Index => "مؤشر",
        _ => Type.ToString()
    };

    public string ChannelArabic => Channel switch
    {
        AlertChannel.App => "التطبيق",
        AlertChannel.WhatsApp => "واتساب",
        AlertChannel.Both => "الكل",
        _ => Channel.ToString()
    };
}

/// <summary>
/// Delivery analytics stats
/// </summary>
public class AlertDeliveryStatsViewModel
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int InactiveAlerts { get; set; }
    public int TotalDeliveries { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal SuccessRate => TotalDeliveries > 0
        ? Math.Round((decimal)SentCount / TotalDeliveries * 100, 1)
        : 0;

    // Channel breakdown
    public int AppChannelCount { get; set; }
    public int WhatsAppChannelCount { get; set; }
    public int BothChannelCount { get; set; }

    // Type breakdown
    public int PriceAlertCount { get; set; }
    public int DisclosureAlertCount { get; set; }
    public int VolumeAlertCount { get; set; }
    public int IndexAlertCount { get; set; }
}
