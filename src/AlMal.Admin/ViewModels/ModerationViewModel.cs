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
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? ReportReason { get; set; }
    public string? ReportedByUserId { get; set; }
    public string? ReportedByDisplayName { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public UserType UserType { get; set; }
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

public class BadgeRequestListViewModel
{
    public List<BadgeRequestItemViewModel> Requests { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

public class BadgeRequestItemViewModel
{
    public long Id { get; set; }
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Email { get; set; }
    public UserType CurrentType { get; set; }
    public UserType RequestedType { get; set; }
    public string? Justification { get; set; }
    public string? CertificateUrl { get; set; }
    public DateTime RequestedAt { get; set; }
    public int PostCount { get; set; }
    public int FollowersCount { get; set; }
}
