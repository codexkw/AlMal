namespace AlMal.Admin.ViewModels;

public class StockListViewModel
{
    public List<StockListItemViewModel> Stocks { get; set; } = [];
    public string? SearchTerm { get; set; }
    public int? SectorFilter { get; set; }
    public List<SectorFilterItem> Sectors { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class StockListItemViewModel
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public string SectorNameAr { get; set; } = null!;
    public decimal? LastPrice { get; set; }
    public bool IsActive { get; set; }
}

public class SectorFilterItem
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
}
