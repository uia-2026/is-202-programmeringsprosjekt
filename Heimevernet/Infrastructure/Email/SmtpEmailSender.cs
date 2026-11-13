using Heimevernet.Infrastructure.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

/// <summary>
/// <see cref="IEmailSender"/> that sends plain text email over SMTP with MailKit.
/// It opens a new connection for each email.
/// </summary>
/// <param name="options">SMTP settings.</param>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when a user name is set but the password is missing.</exception>
    public async Task SendAsync(string toAddress, string toName, string subject, string body, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Heimevernet", _options.FromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _options.Host,
            _options.Port,
            _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            if (string.IsNullOrEmpty(_options.Password))
            {
                throw new InvalidOperationException("Email:Username is set but Email:Password is missing.");
            }

            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}