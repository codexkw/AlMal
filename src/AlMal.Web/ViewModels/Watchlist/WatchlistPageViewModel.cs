namespace AlMal.Web.ViewModels.Watchlist;

public class WatchlistPageViewModel
{
    public List<WatchlistItemViewModel> Items { get; set; } = [];
}

public class WatchlistItemViewModel
{
    public int WatchlistId { get; set; }
    public int StockId { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string SectorNameAr { get; set; } = null!;
    public decimal? LastPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public long? Volume { get; set; }
    public decimal? AlertPrice { get; set; }
    public bool AlertEnabled { get; set; }
}
