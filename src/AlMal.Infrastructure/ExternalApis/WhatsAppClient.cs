using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AlMal.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AlMal.Infrastructure.ExternalApis;

/// <summary>
/// WhatsApp Business Cloud API client using Meta Graph API.
/// Sends text messages, alert templates, and verification codes.
/// </summary>
public class WhatsAppClient : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppClient> _logger;

    private string PhoneNumberId => _configuration["WhatsApp:PhoneNumberId"] ?? string.Empty;
    private string AccessToken => _configuration["WhatsApp:AccessToken"] ?? string.Empty;
    private string ApiBaseUrl => $"https://graph.facebook.com/v18.0/{PhoneNumberId}/messages";

    public WhatsAppClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WhatsAppClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(string phoneNumber, string message)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("WhatsApp API is not configured. Skipping message to {Phone}", MaskPhone(phoneNumber));
            return;
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { body = message }
        };

        await SendPayloadAsync(payload);
    }

    /// <inheritdoc />
    public async Task SendAlertAsync(string phoneNumber, string alertType, Dictionary<string, string> parameters)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("WhatsApp API is not configured. Skipping alert to {Phone}", MaskPhone(phoneNumber));
            return;
        }

        // Build Arabic alert message based on type
        var message = BuildAlertMessage(alertType, parameters);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { body = message }
        };

        await SendPayloadAsync(payload);
        _logger.LogInformation("Sent {AlertType} alert to {Phone}", alertType, MaskPhone(phoneNumber));
    }

    /// <inheritdoc />
    public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string code)
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("WhatsApp API is not configured. Skipping verification to {Phone}", MaskPhone(phoneNumber));
            return false;
        }

        var message = $"\U0001f512 رمز التحقق الخاص بك في قناة المال: {code}\n\nلا تشارك هذا الرمز مع أي شخص.";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = NormalizePhone(phoneNumber),
            type = "text",
            text = new { body = message }
        };

        try
        {
            await SendPayloadAsync(payload);
            _logger.LogInformation("Sent verification code to {Phone}", MaskPhone(phoneNumber));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification code to {Phone}", MaskPhone(phoneNumber));
            return false;
        }
    }

    /// <summary>
    /// Builds a formatted Arabic alert message based on alert type and parameters.
    /// </summary>
    private static string BuildAlertMessage(string alertType, Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("\U0001f4ca قناة المال - تنبيه");
        sb.AppendLine("─────────────────");

        switch (alertType.ToLowerInvariant())
        {
            case "price":
                sb.AppendLine("\U0001f4b0 تنبيه سعر");
                if (parameters.TryGetValue("StockName", out var stockName))
                    sb.AppendLine($"السهم: {stockName}");
                if (parameters.TryGetValue("Symbol", out var symbol))
                    sb.AppendLine($"الرمز: {symbol}");
                if (parameters.TryGetValue("CurrentPrice", out var price))
                    sb.AppendLine($"السعر الحالي: {price} د.ك");
                if (parameters.TryGetValue("TargetPrice", out var target))
                    sb.AppendLine($"السعر المستهدف: {target} د.ك");
                if (parameters.TryGetValue("Condition", out var condition))
                    sb.AppendLine($"الشرط: {condition}");
                break;

            case "disclosure":
                sb.AppendLine("\U0001f4c4 إفصاح جديد");
                if (parameters.TryGetValue("StockName", out var dStockName))
                    sb.AppendLine($"الشركة: {dStockName}");
                if (parameters.TryGetValue("Title", out var title))
                    sb.AppendLine($"العنوان: {title}");
                if (parameters.TryGetValue("Date", out var date))
                    sb.AppendLine($"التاريخ: {date}");
                break;

            case "index":
                sb.AppendLine("\U0001f4c8 تحرك المؤشر");
                if (parameters.TryGetValue("IndexName", out var indexName))
                    sb.AppendLine($"المؤشر: {indexName}");
                if (parameters.TryGetValue("Value", out var value))
                    sb.AppendLine($"القيمة: {value}");
                if (parameters.TryGetValue("ChangePercent", out var changePercent))
                    sb.AppendLine($"نسبة التغير: {changePercent}%");
                break;

            case "volume":
                sb.AppendLine("\U0001f4ca حجم تداول غير اعتيادي");
                if (parameters.TryGetValue("StockName", out var vStockName))
                    sb.AppendLine($"السهم: {vStockName}");
                if (parameters.TryGetValue("Symbol", out var vSymbol))
                    sb.AppendLine($"الرمز: {vSymbol}");
                if (parameters.TryGetValue("Volume", out var volume))
                    sb.AppendLine($"الحجم اليوم: {volume}");
                if (parameters.TryGetValue("AvgVolume", out var avgVolume))
                    sb.AppendLine($"المتوسط (30 يوم): {avgVolume}");
                break;

            default:
                sb.AppendLine(parameters.TryGetValue("Message", out var msg)
                    ? msg
                    : "تنبيه من قناة المال");
                break;
        }

        sb.AppendLine("─────────────────");
        sb.AppendLine("هذا تحليل تعليمي وليس نصيحة استثمارية");
        return sb.ToString();
    }

    /// <summary>
    /// Sends a JSON payload to the WhatsApp Business Cloud API.
    /// </summary>
    private async Task SendPayloadAsync(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccessToken);

        var response = await _httpClient.PostAsync(ApiBaseUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "WhatsApp API returned {StatusCode}: {Body}",
                response.StatusCode, responseBody);
            throw new InvalidOperationException(
                $"WhatsApp API error: {response.StatusCode}");
        }

        _logger.LogDebug("WhatsApp API response: {Body}", responseBody);
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(PhoneNumberId) &&
               !string.IsNullOrWhiteSpace(AccessToken);
    }

    /// <summary>
    /// Normalizes phone number to international format without '+' prefix.
    /// </summary>
    private static string NormalizePhone(string phoneNumber)
    {
        var normalized = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (normalized.StartsWith("+"))
            normalized = normalized[1..];
        return normalized;
    }

    /// <summary>
    /// Masks a phone number for logging purposes.
    /// </summary>
    private static string MaskPhone(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
            return "****";
        return new string('*', phoneNumber.Length - 4) + phoneNumber[^4..];
    }
}
