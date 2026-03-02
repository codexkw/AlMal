using System.Text;
using System.Xml.Linq;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class SitemapController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly IConfiguration _configuration;

    public SitemapController(AlMalDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /sitemap.xml — Generates XML sitemap for search engines
    /// </summary>
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Index()
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://almal.codexkw.co";

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urls = new List<XElement>();

        // Static pages
        urls.Add(CreateUrlElement(ns, baseUrl, "/", "daily", "1.0"));
        urls.Add(CreateUrlElement(ns, baseUrl, "/market", "hourly", "1.0"));
        urls.Add(CreateUrlElement(ns, baseUrl, "/News", "hourly", "0.9"));
        urls.Add(CreateUrlElement(ns, baseUrl, "/Community", "hourly", "0.8"));
        urls.Add(CreateUrlElement(ns, baseUrl, "/Academy", "weekly", "0.7"));

        // Stock pages
        var stocks = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Symbol, s.UpdatedAt })
            .ToListAsync();

        foreach (var stock in stocks)
        {
            var lastMod = stock.UpdatedAt?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            urls.Add(CreateUrlElement(ns, baseUrl, $"/Stock/{stock.Symbol}", "daily", "0.8", lastMod));
        }

        // News articles (last 500)
        var newsArticles = await _context.NewsArticles
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(500)
            .Select(n => new { n.Id, n.CreatedAt })
            .ToListAsync();

        foreach (var article in newsArticles)
        {
            var lastMod = article.CreatedAt.ToString("yyyy-MM-dd");
            urls.Add(CreateUrlElement(ns, baseUrl, $"/News/Detail/{article.Id}", "weekly", "0.6", lastMod));
        }

        // Academy courses
        var courses = await _context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.UpdatedAt })
            .ToListAsync();

        foreach (var course in courses)
        {
            var lastMod = course.UpdatedAt?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
            urls.Add(CreateUrlElement(ns, baseUrl, $"/Academy/Course/{course.Id}", "weekly", "0.6", lastMod));
        }

        var sitemap = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset", urls));

        return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
    }

    private static XElement CreateUrlElement(XNamespace ns, string baseUrl, string path, string changeFreq, string priority, string? lastMod = null)
    {
        var element = new XElement(ns + "url",
            new XElement(ns + "loc", baseUrl + path),
            new XElement(ns + "changefreq", changeFreq),
            new XElement(ns + "priority", priority));

        if (!string.IsNullOrEmpty(lastMod))
        {
            element.Add(new XElement(ns + "lastmod", lastMod));
        }

        return element;
    }
}
