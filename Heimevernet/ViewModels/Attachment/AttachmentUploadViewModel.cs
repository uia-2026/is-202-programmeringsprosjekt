using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Attachment;

/// <summary>
/// Form data for uploading a photo to an existing resource.
/// </summary>
public class AttachmentUploadViewModel
{
    /// <summary>ID of the resource the photo is for. Required.</summary>
    [Required] public int ResourceId { get; set; }
    /// <summary>The image file. Required. Type and size are checked by the attachment service (JPEG, PNG or WebP, max 5 MB).</summary>
    [Required] public IFormFile File { get; set; } = null!;
}