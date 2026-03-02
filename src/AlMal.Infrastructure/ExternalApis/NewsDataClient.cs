using System.Text.Json;
using System.Text.Json.Serialization;
using AlMal.Application.DTOs.News;
using AlMal.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.ExternalApis;

/// <summary>
/// Fetches news articles from the NewsData.io API (Kuwait business news in Arabic).
/// </summary>
public sealed class NewsDataClient : INewsProvider
{
    private const string BaseUrl = "https://newsdata.io/api/1/news";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewsDataClient> _logger;

    public NewsDataClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NewsDataClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<NewsArticleData>> FetchLatestNewsAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["ExternalApis:NewsData:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("NewsData API key is not configured. Set 'ExternalApis:NewsData:ApiKey' in appsettings.");
            return [];
        }

        var url = $"{BaseUrl}?country=kw&language=ar&category=business&apikey={apiKey}";

        try
        {
            using var client = _httpClientFactory.CreateClient("NewsDataClient");
            var response = await client.GetAsync(url, cancellationToken);

            if ((int)response.StatusCode == 429)
            {
                _logger.LogWarning("NewsData API rate limit exceeded. Will retry on next scheduled run.");
                return [];
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<NewsDataApiResponse>(json, JsonOptions);

            if (apiResponse is null || apiResponse.Status != "success")
            {
                _logger.LogWarning("NewsData API returned non-success status. Response: {Status}", apiResponse?.Status ?? "null");
                return [];
            }

            if (apiResponse.Results is null || apiResponse.Results.Count == 0)
            {
                _logger.LogInformation("NewsData API returned 0 articles.");
                return [];
            }

            var articles = new List<NewsArticleData>();
            foreach (var result in apiResponse.Results)
            {
                try
                {
                    var title = result.Title;
                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    var publishedAt = DateTime.TryParse(result.PubDate, out var dt)
                        ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                        : DateTime.UtcNow;

                    var source = result.SourceId ?? result.SourceName ?? "Unknown";

                    articles.Add(new NewsArticleData(
                        TitleAr: title,
                        Source: source,
                        SourceUrl: result.Link,
                        PublishedAt: publishedAt,
                        ExternalId: result.ArticleId,
                        ImageUrl: result.ImageUrl,
                        ContentAr: result.Content ?? result.Description));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse a news article from NewsData API response.");
                }
            }

            _logger.LogInformation("NewsData API returned {Count} articles.", articles.Count);
            return articles;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching news from NewsData API.");
            return [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize NewsData API response.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching news from NewsData API.");
            return [];
        }
    }

    // ------------------------------------------------------------------ //
    //  JSON Deserialization Models (internal to this client)
    // ------------------------------------------------------------------ //

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class NewsDataApiResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("totalResults")]
        public int TotalResults { get; set; }

        [JsonPropertyName("results")]
        public List<NewsDataArticle>? Results { get; set; }

        [JsonPropertyName("nextPage")]
        public string? NextPage { get; set; }
    }

    private sealed class NewsDataArticle
    {
        [JsonPropertyName("article_id")]
        public string? ArticleId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("pubDate")]
        public string? PubDate { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("source_name")]
        public string? SourceName { get; set; }

        [JsonPropertyName("source_url")]
        public string? SourceUrl { get; set; }

        [JsonPropertyName("category")]
        public List<string>? Category { get; set; }

        [JsonPropertyName("country")]
        public List<string>? Country { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }
    }
}
