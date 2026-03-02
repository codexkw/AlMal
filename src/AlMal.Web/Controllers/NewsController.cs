using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using AlMal.Web.ViewModels.News;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Web.Controllers;

public class NewsController : Controller
{
    private readonly AlMalDbContext _context;
    private const int PageSize = 20;

    public NewsController(AlMalDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? stock,
        int? sector,
        string? sentiment,
        int page = 1)
    {
        var viewModel = await BuildNewsFeedAsync(stock, sector, sentiment, page);
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Feed(
        string? stock,
        int? sector,
        string? sentiment,
        int page = 1)
    {
        var viewModel = await BuildNewsFeedAsync(stock, sector, sentiment, page);
        return PartialView("_NewsFeed", viewModel);
    }

    private async Task<NewsFeedViewModel> BuildNewsFeedAsync(
        string? stock,
        int? sector,
        string? sentiment,
        int page)
    {
        // Base query
        var query = _context.NewsArticles
            .AsNoTracking()
            .Include(n => n.NewsArticleStocks)
                .ThenInclude(nas => nas.Stock)
            .AsQueryable();

        // Filter by stock symbol
        if (!string.IsNullOrWhiteSpace(stock))
        {
            query = query.Where(n =>
                n.NewsArticleStocks.Any(nas =>
                    nas.Stock.Symbol.Contains(stock) ||
                    nas.Stock.NameAr.Contains(stock)));
        }

        // Filter by sector
        if (sector.HasValue)
        {
            query = query.Where(n =>
                n.NewsArticleStocks.Any(nas =>
                    nas.Stock.SectorId == sector.Value));
        }

        // Filter by sentiment
        if (!string.IsNullOrWhiteSpace(sentiment) && Enum.TryParse<Sentiment>(sentiment, true, out var sentimentEnum))
        {
            query = query.Where(n => n.Sentiment == sentimentEnum);
        }

        // Order by PublishedAt descending
        query = query.OrderByDescending(n => n.PublishedAt);

        // Pagination
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

        var articles = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(n => new NewsCardViewModel
            {
                Id = n.Id,
                TitleAr = n.TitleAr,
                Source = n.Source,
                SourceUrl = n.SourceUrl,
                Sentiment = n.Sentiment,
                Summary = n.Summary,
                ImageUrl = n.ImageUrl,
                PublishedAt = n.PublishedAt,
                IsProcessed = n.IsProcessed,
                RelatedStocks = n.NewsArticleStocks.Select(nas => new RelatedStockTag
                {
                    Symbol = nas.Stock.Symbol,
                    NameAr = nas.Stock.NameAr
                }).ToList()
            })
            .ToListAsync();

        // Load sectors for filter dropdown
        var sectors = await _context.Sectors
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .Select(s => new SectorFilterOption
            {
                Id = s.Id,
                NameAr = s.NameAr
            })
            .ToListAsync();

        return new NewsFeedViewModel
        {
            Articles = articles,
            Sectors = sectors,
            StockFilter = stock,
            SectorFilter = sector,
            SentimentFilter = sentiment,
            Page = page,
            TotalPages = totalPages
        };
    }
}
