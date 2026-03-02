using AlMal.Domain.Enums;

namespace AlMal.Domain.Entities;

public class Disclosure : BaseEntity
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string TitleAr { get; set; } = null!;
    public string? ContentAr { get; set; }
    public DisclosureType Type { get; set; }
    public DateTime PublishedDate { get; set; }
    public string? SourceUrl { get; set; }
    public string? AiSummary { get; set; }
    public bool IsProcessed { get; set; }

    // Navigation
    public Stock Stock { get; set; } = null!;
}
