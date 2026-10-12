namespace Heimevernet.Models;

/// <summary>
/// Represents a file attachment uploaded for a resource.
/// Stores file path and uploader metadata.
/// </summary>
public class Attachment
{
    public int Id { get; set; }

    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    /// <summary>Object key within the storage bucket.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }
    public User UploadedByUser { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
