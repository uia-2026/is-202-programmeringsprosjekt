namespace Heimevernet.Models;


public class Attachment
{
    public int Id { get; set; }

    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
