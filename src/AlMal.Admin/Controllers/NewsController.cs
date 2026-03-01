using AlMal.Admin.ViewModels;
using AlMal.Domain.Enums;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class NewsController : Controller
{
    private readonly AlMalDbContext _context;
    private readonly ILogger<NewsController> _logger;
    private const int PageSize = 20;

    public NewsController(AlMalDbContext context, ILogger<NewsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, string? source, string? sentiment, bool? processed, int page = 1)
    {
        var query = _context.NewsArticles
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(n => n.TitleAr.Contains(search) ||
                                     (n.Summary != null && n.Summary.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(n => n.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(sentiment) && Enum.TryParse<Sentiment>(sentiment, out var sentimentValue))
        {
            query = query.Where(n => n.Sentiment == sentimentValue);
        }

        if (processed.HasValue)
        {
            query = query.Where(n => n.IsProcessed == processed.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page < 1) page = 1;
        if (page > totalPages && totalPages > 0) page = totalPages;

        var items = await query
            .OrderByDescending(n => n.PublishedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(n => new NewsListItemViewModel
            {
                Id = n.Id,
                TitleAr = n.TitleAr,
                Source = n.Source,
                Sentiment = n.Sentiment,
                IsProcessed = n.IsProcessed,
                PublishedAt = n.PublishedAt,
                HasSummary = n.Summary != null
            })
            .ToListAsync();

        var availableSources = await _context.NewsArticles
            .AsNoTracking()
            .Select(n => n.Source)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        var viewModel = new NewsListViewModel
        {
            Items = items,
            SearchTerm = search,
            SourceFilter = source,
            SentimentFilter = sentiment,
            ProcessedFilter = processed,
            AvailableSources = availableSources,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Detail(long id)
    {
        var article = await _context.NewsArticles
            .AsNoTracking()
            .Include(n => n.NewsArticleStocks)
                .ThenInclude(nas => nas.Stock)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (article == null)
            return NotFound();

        var viewModel = new NewsDetailViewModel
        {
            Id = article.Id,
            TitleAr = article.TitleAr,
            Source = article.Source,
            SourceUrl = article.SourceUrl,
            Sentiment = article.Sentiment,
            Summary = article.Summary,
            ContextData = article.ContextData,
            PublishedAt = article.PublishedAt,
            ImageUrl = article.ImageUrl,
            IsProcessed = article.IsProcessed,
            RelatedStocks = article.NewsArticleStocks
                .Select(nas => new RelatedStockViewModel
                {
                    Symbol = nas.Stock.Symbol,
                    NameAr = nas.Stock.NameAr
                })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideSentiment(long id, Sentiment sentiment)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article == null)
            return NotFound();

        var oldSentiment = article.Sentiment;
        article.Sentiment = sentiment;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "News article sentiment overridden: ID {Id}, from {OldSentiment} to {NewSentiment}",
            id, oldSentiment, sentiment);

        TempData["Success"] = "تم تحديث التصنيف بنجاح";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reprocess(long id)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article == null)
            return NotFound();

        article.IsProcessed = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("News article marked for reprocessing: ID {Id}", id);

        TempData["Success"] = "تم وضع علامة لإعادة المعالجة";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var article = await _context.NewsArticles.FindAsync(id);
        if (article == null)
            return NotFound();

        _context.NewsArticles.Remove(article);
        await _context.SaveChangesAsync();

        _logger.LogInformation("News article deleted: ID {Id}, Title: {Title}", id, article.TitleAr);

        TempData["Success"] = "تم حذف الخبر بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
