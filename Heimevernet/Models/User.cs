namespace Heimevernet.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public string ActorType { get; set; } = string.Empty;

    public bool TwoFactorEnabled { get; set; }

    public string? TwoFactorSecret { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Need> Needs { get; set; } = new List<Need>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public ICollection<Attachment> UploadedAttachments { get; set; } = new List<Attachment>();
    public ICollection<Match> MatchesCreated { get; set; } = new List<Match>();
}