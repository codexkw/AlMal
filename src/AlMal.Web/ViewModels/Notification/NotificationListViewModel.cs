using AlMal.Domain.Enums;

namespace AlMal.Web.ViewModels.Notification;

public class NotificationListViewModel
{
    public List<NotificationItemViewModel> Notifications { get; set; } = new List<NotificationItemViewModel>();
    public int UnreadCount { get; set; }
}

public class NotificationItemViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string TypeIcon => Type switch
    {
        NotificationType.PriceAlert => "bi-currency-exchange",
        NotificationType.NewDisclosure => "bi-file-earmark-text",
        NotificationType.NewFollower => "bi-person-plus",
        NotificationType.PostLike => "bi-heart",
        NotificationType.PostComment => "bi-chat-dots",
        NotificationType.CourseComplete => "bi-mortarboard",
        NotificationType.CertificateIssued => "bi-award",
        NotificationType.System => "bi-info-circle",
        _ => "bi-bell"
    };
    public string? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo => GetTimeAgo(CreatedAt);

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "الآن";
        if (span.TotalMinutes < 60) return $"منذ {(int)span.TotalMinutes} دقيقة";
        if (span.TotalHours < 24) return $"منذ {(int)span.TotalHours} ساعة";
        if (span.TotalDays < 7) return $"منذ {(int)span.TotalDays} يوم";
        return dateTime.ToString("yyyy/MM/dd");
    }
}
