namespace AlMal.Admin.ViewModels;

public class SectorListViewModel
{
    public List<SectorListItemViewModel> Sectors { get; set; } = [];
}

public class SectorListItemViewModel
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public bool IsActive { get; set; }
    public int StockCount { get; set; }
}
