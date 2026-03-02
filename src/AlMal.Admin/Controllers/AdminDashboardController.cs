using AlMal.Admin.ViewModels;
using AlMal.Domain.Entities;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("Dashboard")]
public class AdminDashboardController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<AdminDashboardController> _logger;

    public AdminDashboardController(AlMalDbContext context, ILogger<AdminDashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /Dashboard — System-wide analytics dashboard
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);

            // User stats
            var usersQuery = _context.Users
                .AsNoTracking()
                .OfType<ApplicationUser>();

            var totalUsers = await usersQuery.CountAsync();
            var activeUsers = await usersQuery.CountAsync(u => u.IsActive);
            var newUsersLast7Days = await usersQuery.CountAsync(u => u.CreatedAt >= sevenDaysAgo);
            var newUsersLast30Days = await usersQuery.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

            // User growth chart — last 30 days
            var userGrowthChart = await usersQuery
                .Where(u => u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new UserGrowthPoint
                {
                    Date = g.Key.ToString("MM/dd"),
                    Count = g.Count()
                })
                .OrderBy(p => p.Date)
                .ToListAsync();

            // Engagement stats
            var totalPosts = await _context.Posts.AsNoTracking().CountAsync(p => !p.IsDeleted);
            var totalComments = await _context.Comments.AsNoTracking().CountAsync();
            var totalLikes = await _context.PostLikes.AsNoTracking().CountAsync();
            var postsLast7Days = await _context.Posts.AsNoTracking()
                .CountAsync(p => !p.IsDeleted && p.CreatedAt >= sevenDaysAgo);

            // Market data stats
            var totalStocks = await _context.Stocks.AsNoTracking().CountAsync();
            var activeStocks = await _context.Stocks.AsNoTracking().CountAsync(s => s.IsActive);
            var totalDisclosures = await _context.Disclosures.AsNoTracking().CountAsync();
            var totalNewsArticles = await _context.NewsArticles.AsNoTracking().CountAsync();
            var newsLast7Days = await _context.NewsArticles.AsNoTracking()
                .CountAsync(n => n.CreatedAt >= sevenDaysAgo);

            // Popular stocks — most watchlisted
            var mostWatchlisted = await _context.Watchlists
                .AsNoTracking()
                .GroupBy(w => w.StockId)
                .Select(g => new { StockId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Join(
                    _context.Stocks.AsNoTracking(),
                    w => w.StockId,
                    s => s.Id,
                    (w, s) => new PopularStockViewModel
                    {
                        StockId = s.Id,
                        Symbol = s.Symbol,
                        NameAr = s.NameAr,
                        Count = w.Count
                    })
                .ToListAsync();

            // Popular stocks — most discussed (stock mentions in posts)
            var mostDiscussed = await _context.PostStockMentions
                .AsNoTracking()
                .GroupBy(m => m.StockId)
                .Select(g => new { StockId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .Join(
                    _context.Stocks.AsNoTracking(),
                    m => m.StockId,
                    s => s.Id,
                    (m, s) => new PopularStockViewModel
                    {
                        StockId = s.Id,
                        Symbol = s.Symbol,
                        NameAr = s.NameAr,
                        Count = m.Count
                    })
                .ToListAsync();

            // Alerts & WhatsApp
            var activeAlertsCount = await _context.Alerts.AsNoTracking().CountAsync(a => a.IsActive);
            var whatsAppOptInCount = await usersQuery.CountAsync(u => u.WhatsAppOptIn);

            // Academy
            var totalCourses = await _context.Courses.AsNoTracking().CountAsync();
            var totalEnrollments = await _context.Enrollments.AsNoTracking().CountAsync();
            var totalCertificates = await _context.Certificates.AsNoTracking().CountAsync();

            var viewModel = new DashboardViewModel
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                NewUsersLast7Days = newUsersLast7Days,
                NewUsersLast30Days = newUsersLast30Days,
                UserGrowthChart = userGrowthChart,
                TotalPosts = totalPosts,
                TotalComments = totalComments,
                TotalLikes = totalLikes,
                PostsLast7Days = postsLast7Days,
                TotalStocks = totalStocks,
                ActiveStocks = activeStocks,
                TotalDisclosures = totalDisclosures,
                TotalNewsArticles = totalNewsArticles,
                NewsLast7Days = newsLast7Days,
                MostWatchlisted = mostWatchlisted,
                MostDiscussed = mostDiscussed,
                ActiveAlertsCount = activeAlertsCount,
                WhatsAppOptInCount = whatsAppOptInCount,
                TotalCourses = totalCourses,
                TotalEnrollments = totalEnrollments,
                TotalCertificates = totalCertificates
            };

            return View("~/Views/Dashboard/Index.cshtml", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            return View("~/Views/Dashboard/Index.cshtml", new DashboardViewModel());
        }
    }
}
