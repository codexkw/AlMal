using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Profile;

public class ProfileViewModel
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; }
    public bool IsVerified { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostCount { get; set; }
    public bool IsOwnProfile { get; set; }
    public bool IsFollowing { get; set; }
    public DateTime MemberSince { get; set; }
}

public class EditProfileViewModel
{
    public string DisplayName { get; set; } = null!;
    public string? Bio { get; set; }
}

public class FollowListViewModel
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string ListType { get; set; } = null!; // "followers" or "following"
    public List<FollowListItem> Users { get; set; } = [];
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
}

public class FollowListItem
{
    public string UserId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; }
    public bool IsFollowing { get; set; }
}
