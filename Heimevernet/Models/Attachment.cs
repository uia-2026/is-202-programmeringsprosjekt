namespace Heimevernet.Models;

/// <summary>
/// Represents a file attachment uploaded for a resource.
/// Stores file path and uploader metadata.
/// </summary>
public class Attachment
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>ID of the resource the image belongs to.</summary>
    public int ResourceId { get; set; }
    /// <summary>The resource the image belongs to.</summary>
    public Resource Resource { get; set; } = null!;

    /// <summary>Object key within the storage bucket.</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>ID of the user who uploaded the image. Only this user may delete it.</summary>
    public int UploadedByUserId { get; set; }
    /// <summary>The user who uploaded the image.</summary>
    public User UploadedByUser { get; set; } = null!;

    /// <summary>When the image was uploaded (UTC).</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}