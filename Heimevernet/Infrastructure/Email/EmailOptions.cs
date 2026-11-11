namespace Heimevernet.Infrastructure.Email;

/// <summary>
/// Settings for sending email. With Enabled set to false nothing is sent and messages are only logged.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "Heimevernet";
}