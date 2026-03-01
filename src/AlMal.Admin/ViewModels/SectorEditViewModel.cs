namespace AlMal.Admin.ViewModels;

public class SectorEditViewModel
{
    public int Id { get; set; }
    public string NameAr { get; set; } = null!;
    public string? NameEn { get; set; }
    public bool IsActive { get; set; } = true;
}
