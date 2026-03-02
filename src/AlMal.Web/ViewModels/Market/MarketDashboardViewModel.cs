namespace AlMal.Web.ViewModels.Market;

public class MarketDashboardViewModel
{
    public List<MarketIndexViewModel> Indices { get; set; } = [];
    public List<StockViewModel> TopGainers { get; set; } = [];
    public List<StockViewModel> TopLosers { get; set; } = [];
    public List<StockViewModel> MostTraded { get; set; } = [];
    public List<StockViewModel> AllStocks { get; set; } = [];
    public List<SectorViewModel> Sectors { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalStocks { get; set; }
    public string? SearchQuery { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public int? SectorFilter { get; set; }
}

public class MarketIndexViewModel
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string Type { get; set; } = null!;
    public decimal Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
}

public class StockViewModel
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string SectorNameAr { get; set; } = null!;
    public int SectorId { get; set; }
    public decimal? LastPrice { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public decimal? MarketCap { get; set; }
    public long? Volume { get; set; }
    public bool IsInWatchlist { get; set; }
}

public class SectorViewModel
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public decimal? IndexValue { get; set; }
    public decimal? ChangePercent { get; set; }
    public int StockCount { get; set; }
}
