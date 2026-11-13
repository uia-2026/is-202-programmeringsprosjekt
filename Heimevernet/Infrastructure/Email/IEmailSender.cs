namespace Heimevernet.Infrastructure.Email;

/// <summary>
/// Sends email. The real implementation is <see cref="SmtpEmailSender"/>; <see cref="LoggingEmailSender"/> is used when email is disabled.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends a plain text email. Throws if the mail server refuses or cannot be reached.</summary>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="subject">Subject line.</param>
    /// <param name="textBody">Plain text body.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the email has been handed to the server.</returns>
    Task SendAsync(string toAddress, string toName, string subject, string textBody, CancellationToken ct = default);
}