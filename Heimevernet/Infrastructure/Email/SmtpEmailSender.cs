using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Heimevernet.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options) => _options = options.Value;

    public async Task SendAsync(string toAddress, string toName, string subject, string textBody, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(toName, toAddress));
        message.Subject = subject.ReplaceLineEndings(" ");        // user text can end up here: no line breaks
        message.Body = new TextPart("plain") { Text = textBody }; // plain text, so user input is never rendered as HTML

        using var client = new SmtpClient { Timeout = 15_000 };   // a new client per message, with a 15 second timeout
        await client.ConnectAsync(_options.Host, _options.Port,
            _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}