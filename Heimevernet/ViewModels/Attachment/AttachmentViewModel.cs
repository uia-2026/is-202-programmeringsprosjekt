namespace Heimevernet.ViewModels.Attachment;

/// <summary>
/// A photo shown on the resource details page.
/// </summary>
public class AttachmentViewModel
{
    /// <summary>ID of the attachment, used by the delete button.</summary>
    public int Id { get; set; }
    /// <summary>URL the browser can load the image from.</summary>
    public string PublicUrl { get; set; } = string.Empty;
}