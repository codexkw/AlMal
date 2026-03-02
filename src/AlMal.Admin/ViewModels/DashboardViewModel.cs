namespace AlMal.Admin.ViewModels;

/// <summary>
/// ViewModel for system-wide analytics dashboard
/// </summary>
public class DashboardViewModel
{
    // User Stats
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersLast7Days { get; set; }
    public int NewUsersLast30Days { get; set; }
    public List<UserGrowthPoint> UserGrowthChart { get; set; } = new List<UserGrowthPoint>();

    // Engagement Stats
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int TotalLikes { get; set; }
    public int PostsLast7Days { get; set; }

    // Market Data Stats
    public int TotalStocks { get; set; }
    public int ActiveStocks { get; set; }
    public int TotalDisclosures { get; set; }
    public int TotalNewsArticles { get; set; }
    public int NewsLast7Days { get; set; }

    // Popular Stocks
    public List<PopularStockViewModel> MostWatchlisted { get; set; } = new List<PopularStockViewModel>();
    public List<PopularStockViewModel> MostDiscussed { get; set; } = new List<PopularStockViewModel>();

    // Alerts & WhatsApp
    public int ActiveAlertsCount { get; set; }
    public int WhatsAppOptInCount { get; set; }

    // Academy
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalCertificates { get; set; }
}

public class UserGrowthPoint
{
    public string Date { get; set; } = null!;
    public int Count { get; set; }
}

public class PopularStockViewModel
{
    public int StockId { get; set; }
    public string Symbol { get; set; } = null!;
    public string NameAr { get; set; } = null!;
    public int Count { get; set; }
}
