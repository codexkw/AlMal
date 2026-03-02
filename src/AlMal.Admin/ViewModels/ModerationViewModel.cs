using AlMal.Domain.Enums;

namespace AlMal.Admin.ViewModels;

public class ModerationQueueViewModel
{
    public List<ModerationItemViewModel> Items { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class ModerationItemViewModel
{
    public long Id { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string UserDisplayName { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class UserManagementViewModel
{
    public List<UserManagementItemViewModel> Users { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? TypeFilter { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class UserManagementItemViewModel
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public UserType UserType { get; set; }
    public bool IsActive { get; set; }
    public int PostCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
