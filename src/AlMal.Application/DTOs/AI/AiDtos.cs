namespace AlMal.Application.DTOs.AI;

/// <summary>
/// Result of an AI-generated explanation for a stock's price movement.
/// </summary>
public class MovementExplanationResult
{
    /// <summary>
    /// The AI-generated explanation in Arabic describing the stock's recent price movement.
    /// </summary>
    public string Explanation { get; set; } = null!;

    /// <summary>
    /// Educational disclaimer (always included). Default: "This is an educational analysis, not investment advice."
    /// </summary>
    public string Disclaimer { get; set; } = "\u0647\u0630\u0627 \u062a\u062d\u0644\u064a\u0644 \u062a\u0639\u0644\u064a\u0645\u064a \u0648\u0644\u064a\u0633 \u0646\u0635\u064a\u062d\u0629 \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629";
}

/// <summary>
/// Result of AI-generated context and sentiment analysis for a news article.
/// </summary>
public class NewsContextResult
{
    /// <summary>
    /// A concise Arabic summary of the news article.
    /// </summary>
    public string Summary { get; set; } = null!;

    /// <summary>
    /// The detected sentiment: "Positive", "Negative", or "Neutral".
    /// </summary>
    public string Sentiment { get; set; } = null!;

    /// <summary>
    /// Additional contextual background information, if available.
    /// </summary>
    public string? ContextData { get; set; }

    /// <summary>
    /// Educational disclaimer (always included).
    /// </summary>
    public string Disclaimer { get; set; } = "\u0647\u0630\u0627 \u062a\u062d\u0644\u064a\u0644 \u062a\u0639\u0644\u064a\u0645\u064a \u0648\u0644\u064a\u0633 \u0646\u0635\u064a\u062d\u0629 \u0627\u0633\u062a\u062b\u0645\u0627\u0631\u064a\u0629";
}
