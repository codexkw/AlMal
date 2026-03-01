namespace AlMal.Application.DTOs.News;

/// <summary>
/// Represents a single news article fetched from an external news provider.
/// </summary>
public record NewsArticleData(
    string TitleAr,
    string Source,
    string? SourceUrl,
    DateTime PublishedAt,
    string? ExternalId,
    string? ImageUrl,
    string? ContentAr);
