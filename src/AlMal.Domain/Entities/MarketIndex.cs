using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class MarketIndex : BaseEntity
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public MarketIndexType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? Change { get; set; }
    public decimal? ChangePercent { get; set; }
    public DateTime Date { get; set; }
}
