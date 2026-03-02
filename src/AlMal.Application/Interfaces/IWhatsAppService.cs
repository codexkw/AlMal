namespace AlMal.Application.Interfaces;

/// <summary>
/// Provides WhatsApp messaging capabilities via the WhatsApp Business Cloud API.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a plain text message to a WhatsApp number.
    /// </summary>
    Task SendMessageAsync(string phoneNumber, string message);

    /// <summary>
    /// Sends a templated alert message (price, disclosure, index, volume) to a WhatsApp number.
    /// </summary>
    Task SendAlertAsync(string phoneNumber, string alertType, Dictionary<string, string> parameters);

    /// <summary>
    /// Sends a verification code via WhatsApp. Returns true if the message was sent successfully.
    /// </summary>
    Task<bool> SendVerificationCodeAsync(string phoneNumber, string code);
}
