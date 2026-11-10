namespace Heimevernet.Infrastructure.Email;

public interface IEmailSender
{
    /// <summary>Sends a plain text email. Throws if the mail server refuses or cannot be reached.</summary>
    Task SendAsync(string toAddress, string toName, string subject, string textBody, CancellationToken ct = default);
}