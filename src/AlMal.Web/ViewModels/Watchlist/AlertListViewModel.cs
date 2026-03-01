using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Watchlist;

public class AlertListViewModel
{
    public List<AlertItemViewModel> Alerts { get; set; } = [];
}

public class AlertItemViewModel
{
    public int AlertId { get; set; }
    public AlertType Type { get; set; }
    public string? StockSymbol { get; set; }
    public string? StockNameAr { get; set; }
    public string Condition { get; set; } = null!;
    public decimal? TargetValue { get; set; }
    public AlertChannel Channel { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastTriggered { get; set; }
    public DateTime CreatedAt { get; set; }
}
