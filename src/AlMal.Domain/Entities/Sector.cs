namespace AlMal.Domain.Entities;

public class Sector : BaseEntity
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public int SortOrder { get; set; }
    public decimal? IndexValue { get; set; }
    public decimal? ChangePercent { get; set; }

    // Navigation
    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
