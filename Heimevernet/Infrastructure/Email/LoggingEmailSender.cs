namespace Heimevernet.Infrastructure.Email;

/// <summary>Used when email is disabled. Logs that a mail would have been sent, and never the address.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toAddress, string toName, string subject, string textBody, CancellationToken ct = default)
    {
        _logger.LogInformation("Email is disabled. Would have sent: {Subject}", subject);
        return Task.CompletedTask;
    }
}