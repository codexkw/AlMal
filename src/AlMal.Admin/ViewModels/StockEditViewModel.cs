namespace AlMal.Admin.ViewModels;

public class StockEditViewModel
{
    public int Id { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public int SectorId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? DescriptionAr { get; set; }
    public long? SharesOutstanding { get; set; }
    public List<SectorFilterItem> Sectors { get; set; } = [];
}
