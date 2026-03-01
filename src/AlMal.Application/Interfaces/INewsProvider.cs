using AlMal.Application.DTOs.News;

namespace AlMal.Application.Interfaces;

/// <summary>
/// Provides methods to fetch news articles from an external source (e.g., NewsData.io).
/// </summary>
public interface INewsProvider
{
    /// <summary>
    /// Fetches the latest business news articles relevant to the Kuwait market.
    /// </summary>
    Task<List<NewsArticleData>> FetchLatestNewsAsync(CancellationToken cancellationToken = default);
}
