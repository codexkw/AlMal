using System.Text.Json;
using AlMal.Application.Interfaces;
using AlMal.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AlMal.Web.Controllers;

/// <summary>
/// Handles WhatsApp Business webhook verification and incoming messages.
/// The GET endpoint verifies the webhook with Meta.
/// The POST endpoint receives incoming messages and replies via AI.
/// </summary>
[Route("webhooks/whatsapp")]
public class WhatsAppWebhookController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IAiAnalysisService _aiService;
    private readonly AlMalDbContext _context;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IConfiguration configuration,
        IWhatsAppService whatsAppService,
        IAiAnalysisService aiService,
        AlMalDbContext context,
        ILogger<WhatsAppWebhookController> logger)
    {
        _configuration = configuration;
        _whatsAppService = whatsAppService;
        _aiService = aiService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// GET /webhooks/whatsapp — Meta webhook verification endpoint.
    /// Meta sends hub.mode, hub.verify_token, and hub.challenge.
    /// </summary>
    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && verifyToken == expectedToken && !string.IsNullOrEmpty(challenge))
        {
            _logger.LogInformation("WhatsApp webhook verified successfully");
            return Ok(challenge);
        }

        _logger.LogWarning("WhatsApp webhook verification failed. Mode: {Mode}, Token match: {Match}",
            mode, verifyToken == expectedToken);
        return Forbid();
    }

    /// <summary>
    /// POST /webhooks/whatsapp — Receive incoming WhatsApp messages.
    /// Parses the message payload, looks up the user, calls AI, and replies.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        string body;
        using (var reader = new StreamReader(Request.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        _logger.LogDebug("WhatsApp webhook received: {Body}", body);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Navigate the Meta webhook payload structure
            if (!root.TryGetProperty("entry", out var entries))
                return Ok();

            foreach (var entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("changes", out var changes))
                    continue;

                foreach (var change in changes.EnumerateArray())
                {
                    if (!change.TryGetProperty("value", out var value))
                        continue;

                    if (!value.TryGetProperty("messages", out var messages))
                        continue;

                    foreach (var message in messages.EnumerateArray())
                    {
                        var messageType = message.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                        if (messageType != "text")
                            continue;

                        var from = message.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : null;
                        var textBody = message.TryGetProperty("text", out var textEl) &&
                                       textEl.TryGetProperty("body", out var bodyEl)
                            ? bodyEl.GetString()
                            : null;

                        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(textBody))
                            continue;

                        await ProcessIncomingMessageAsync(from, textBody);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WhatsApp webhook");
        }

        // Always return 200 to acknowledge receipt
        return Ok();
    }

    /// <summary>
    /// Processes an incoming text message: looks up user, calls AI, sends reply.
    /// </summary>
    private async Task ProcessIncomingMessageAsync(string phoneNumber, string question)
    {
        _logger.LogInformation("Processing WhatsApp message from {Phone}: {Question}",
            MaskPhone(phoneNumber), question.Length > 50 ? question[..50] + "..." : question);

        // Look up user by phone number
        var normalizedPhone = NormalizePhone(phoneNumber);
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.WhatsAppNumber != null &&
                u.WhatsAppOptIn &&
                u.WhatsAppNumber.Replace("+", "").Replace(" ", "").Replace("-", "") == normalizedPhone);

        string? userId = user?.Id;

        if (user == null)
        {
            // Send a message asking the user to register
            var registerMsg = "مرحباً بك في قناة المال!\n\n" +
                              "للاستفادة من خدمة المساعد الذكي، يرجى التسجيل في المنصة وربط رقم واتساب الخاص بك.\n\n" +
                              "قناة المال - بورصة الكويت";
            await _whatsAppService.SendMessageAsync(phoneNumber, registerMsg);
            return;
        }

        try
        {
            // Call AI to answer the question
            var response = await _aiService.AnswerMarketQuestionAsync(question, userId);

            // Send the AI response back via WhatsApp
            await _whatsAppService.SendMessageAsync(phoneNumber, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for WhatsApp question");
            await _whatsAppService.SendMessageAsync(phoneNumber,
                "عذراً، حدث خطأ أثناء معالجة سؤالك. حاول مرة أخرى لاحقاً.\n\nقناة المال");
        }
    }

    private static string NormalizePhone(string phoneNumber)
    {
        return phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
    }

    private static string MaskPhone(string phoneNumber)
    {
        if (phoneNumber.Length <= 4) return "****";
        return new string('*', phoneNumber.Length - 4) + phoneNumber[^4..];
    }
}
