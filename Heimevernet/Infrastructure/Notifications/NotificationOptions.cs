namespace Heimevernet.Infrastructure.Notifications;

/// <summary>Who gets notification emails, and how many. Tuned for free email plans with a daily cap.</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>How far from a new critical need a provider's resource can be and still trigger an email.</summary>
    public double CriticalNeedRadiusKm { get; set; } = 50;

    /// <summary>How far from a new resource a critical need can be and still trigger an email.</summary>
    public double ResourceNotificationRadiusKm { get; set; } = 50;

    /// <summary>The most people emailed about one event (a new critical need or a new resource).</summary>
    public int MaxRecipientsPerEvent { get; set; } = 20;

    /// <summary>The most notification emails one user receives in 24 hours, across all email types.</summary>
    public int MaxEmailsPerUserPerDay { get; set; } = 5;

    /// <summary>Address of the web application, used for links in emails.</summary>
    public string PublicBaseUrl { get; set; } = "";
}