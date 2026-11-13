namespace Heimevernet.Models;

/// <summary>
/// Domain model representing an application user.
/// Stores authentication and profile information along with navigation properties.
/// </summary>
public class User
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name used in the app and audit log. Unique.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address used for login and notifications. Unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hashed password from ASP.NET Core's password hasher. Never the plain password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>The user's role, which decides what they can do.</summary>
    public UserRole Role { get; set; }

    /// <summary>What kind of actor the user is, e.g. "Municipality" or "Volunteer".</summary>
    public string ActorType { get; set; } = string.Empty;

    /// <summary><see langword="true"/> when the user must enter a 2FA code at login.</summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>Base32 TOTP secret for 2FA, or <see langword="null"/> when 2FA is not set up.</summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>Whether the user wants notification emails. On by default.</summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Random value that changes when the password changes. Sign-in cookies with an old stamp are no longer accepted.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>When the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Needs registered by the user.</summary>
    public ICollection<Need> Needs { get; set; } = [];
    /// <summary>Resources registered by the user.</summary>
    public ICollection<Resource> Resources { get; set; } = [];
    /// <summary>Photos uploaded by the user.</summary>
    public ICollection<Attachment> UploadedAttachments { get; set; } = [];
    /// <summary>Matches proposed by the user.</summary>
    public ICollection<Match> MatchesCreated { get; set; } = [];
    /// <summary>Audit entries for actions done by the user.</summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}