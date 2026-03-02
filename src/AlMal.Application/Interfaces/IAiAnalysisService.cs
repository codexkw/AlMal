using AlMal.Application.DTOs.AI;

namespace AlMal.Application.Interfaces;

/// <summary>
/// Provides AI-powered analysis services for market data, disclosures, and news.
/// </summary>
public interface IAiAnalysisService
{
    /// <summary>
    /// Generates an AI explanation for a stock's recent price movement.
    /// Uses cached data from Redis (TTL 30 min) and recent OHLCV + disclosure data.
    /// </summary>
    Task<MovementExplanationResult> ExplainMovementAsync(string symbol, CancellationToken ct = default);

    /// <summary>
    /// Summarizes a disclosure in 2-3 Arabic sentences, focusing on shareholder impact.
    /// </summary>
    Task<string> SummarizeDisclosureAsync(string disclosureContentAr, CancellationToken ct = default);

    /// <summary>
    /// Generates contextual analysis, sentiment, and background for a news article.
    /// </summary>
    Task<NewsContextResult> GenerateNewsContextAsync(long newsArticleId, CancellationToken ct = default);

    /// <summary>
    /// Answers a market-related question from a user (WhatsApp Market Assistant).
    /// Builds context from latest stock prices, recent disclosures, and news.
    /// </summary>
    Task<string> AnswerMarketQuestionAsync(string question, string? userId = null, CancellationToken ct = default);
}
