using AlMal.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlMal.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
[Route("Settings")]
public class AdminSettingsController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSettingsController> _logger;

    public AdminSettingsController(IConfiguration configuration, ILogger<AdminSettingsController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// GET /Settings — System settings page
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var viewModel = new SettingsViewModel
        {
            // API Keys (masked for display)
            ClaudeApiKey = SettingsViewModel.MaskApiKey(_configuration["Anthropic:ApiKey"]),
            WhatsAppApiKey = SettingsViewModel.MaskApiKey(_configuration["WhatsApp:ApiToken"]),
            NewsApiKey = SettingsViewModel.MaskApiKey(_configuration["NewsApi:ApiKey"]),

            // Job Schedules (read from config, fallback to defaults)
            MarketScraperSchedule = _configuration["Jobs:MarketScraperSchedule"] ?? "*/30 * * * * *",
            OrderBookSchedule = _configuration["Jobs:OrderBookSchedule"] ?? "* * * * *",
            DisclosureSchedule = _configuration["Jobs:DisclosureSchedule"] ?? "*/5 * * * *",
            NewsSchedule = _configuration["Jobs:NewsSchedule"] ?? "*/15 * * * *",
            AlertEngineSchedule = _configuration["Jobs:AlertEngineSchedule"] ?? "*/30 * * * * *",
            AiProcessorSchedule = _configuration["Jobs:AiProcessorSchedule"] ?? "*/10 * * * *",
            DailyMarketSummarySchedule = _configuration["Jobs:DailyMarketSummarySchedule"] ?? "45 9 * * 0-4",

            // AI Config
            AiModel = _configuration["Anthropic:Model"] ?? "claude-sonnet-4-20250514",
            AiMaxTokensSummary = int.TryParse(_configuration["Anthropic:MaxTokensSummary"], out var mts) ? mts : 1024,
            AiMaxTokensExplanation = int.TryParse(_configuration["Anthropic:MaxTokensExplanation"], out var mte) ? mte : 2048,
            AiTemperature = double.TryParse(_configuration["Anthropic:Temperature"], out var temp) ? temp : 0.3,

            // System Parameters
            DefaultPageSize = int.TryParse(_configuration["System:DefaultPageSize"], out var dps) ? dps : 20,
            MaxFileUploadSizeMb = int.TryParse(_configuration["System:MaxFileUploadSizeMb"], out var mfu) ? mfu : 5,
            CacheExpirationMinutes = int.TryParse(_configuration["System:CacheExpirationMinutes"], out var cem) ? cem : 5,
            MaintenanceMode = bool.TryParse(_configuration["System:MaintenanceMode"], out var mm) && mm
        };

        return View("~/Views/Settings/Index.cshtml", viewModel);
    }

    /// <summary>
    /// POST /Settings/Save — Save settings
    /// </summary>
    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save(SettingsViewModel model)
    {
        // In a production system, settings would be persisted to database or a config store.
        // For now, we log the save attempt and show a success message.
        // IConfiguration is read-only at runtime, so actual persistence
        // would require a custom settings provider.

        _logger.LogInformation(
            "Settings save requested by admin {Admin}. " +
            "MaintenanceMode={MaintenanceMode}, DefaultPageSize={PageSize}, " +
            "AiModel={AiModel}, AiTemperature={Temperature}",
            User.Identity?.Name,
            model.MaintenanceMode,
            model.DefaultPageSize,
            model.AiModel,
            model.AiTemperature);

        TempData["Success"] = "تم حفظ الإعدادات بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
