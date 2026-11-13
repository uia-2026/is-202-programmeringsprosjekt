namespace Heimevernet.Infrastructure.Email;

/// <summary>
/// Settings for sending email. With Enabled set to false nothing is sent and messages are only logged.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>Name of the configuration section the options are read from.</summary>
    public const string SectionName = "Email";

    /// <summary>When <see langword="false"/>, no email is sent and the outbox worker does not start.</summary>
    public bool Enabled { get; set; }
    /// <summary>SMTP server host name. Required when <see cref="Enabled"/> is <see langword="true"/>.</summary>
    public string Host { get; set; } = "";
    /// <summary>SMTP server port. Defaults to 587.</summary>
    public int Port { get; set; } = 587;
    /// <summary>Whether to upgrade the connection with STARTTLS. On by default.</summary>
    public bool UseStartTls { get; set; } = true;
    /// <summary>SMTP user name, or <see langword="null"/> when the server needs no login.</summary>
    public string? Username { get; set; }
    /// <summary>SMTP password. Required when <see cref="Username"/> is set.</summary>
    public string? Password { get; set; }
    /// <summary>Sender address. Required when <see cref="Enabled"/> is <see langword="true"/>.</summary>
    public string FromAddress { get; set; } = "";
    /// <summary>Sender display name. Defaults to "Heimevernet".</summary>
    public string FromName { get; set; } = "Heimevernet";
}