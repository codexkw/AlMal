using System.ComponentModel.DataAnnotations;
using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Alert;

public class CreateAlertViewModel
{
    [Required(ErrorMessage = "نوع التنبيه مطلوب")]
    public AlertType Type { get; set; }

    public int? StockId { get; set; }

    public string? StockSearch { get; set; }

    [Required(ErrorMessage = "الشرط مطلوب")]
    public string Condition { get; set; } = "above";

    public decimal? TargetValue { get; set; }

    public AlertChannel Channel { get; set; } = AlertChannel.Both;

    /// <summary>
    /// Available stocks for the dropdown (populated by controller).
    /// </summary>
    public List<StockOptionViewModel> AvailableStocks { get; set; } = new List<StockOptionViewModel>();
}

public class StockOptionViewModel
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
}
