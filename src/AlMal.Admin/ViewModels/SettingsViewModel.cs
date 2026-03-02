namespace AlMal.Admin.ViewModels;

/// <summary>
/// ViewModel for system settings page (SuperAdmin only)
/// </summary>
public class SettingsViewModel
{
    // API Keys (masked)
    public string? ClaudeApiKey { get; set; }
    public string? WhatsAppApiKey { get; set; }
    public string? NewsApiKey { get; set; }

    // Job Schedules
    public string MarketScraperSchedule { get; set; } = "*/30 * * * * *";
    public string OrderBookSchedule { get; set; } = "* * * * *";
    public string DisclosureSchedule { get; set; } = "*/5 * * * *";
    public string NewsSchedule { get; set; } = "*/15 * * * *";
    public string AlertEngineSchedule { get; set; } = "*/30 * * * * *";
    public string AiProcessorSchedule { get; set; } = "*/10 * * * *";
    public string DailyMarketSummarySchedule { get; set; } = "45 9 * * 0-4";

    // AI Config
    public string AiModel { get; set; } = "claude-sonnet-4-20250514";
    public int AiMaxTokensSummary { get; set; } = 1024;
    public int AiMaxTokensExplanation { get; set; } = 2048;
    public double AiTemperature { get; set; } = 0.3;

    // System Parameters
    public int DefaultPageSize { get; set; } = 20;
    public int MaxFileUploadSizeMb { get; set; } = 5;
    public int CacheExpirationMinutes { get; set; } = 5;
    public bool MaintenanceMode { get; set; }

    /// <summary>
    /// Mask an API key for display (show first 4 and last 4 chars)
    /// </summary>
    public static string MaskApiKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "غير مُعيَّن";
        if (key.Length <= 8) return new string('*', key.Length);
        return key[..4] + new string('*', key.Length - 8) + key[^4..];
    }
}
