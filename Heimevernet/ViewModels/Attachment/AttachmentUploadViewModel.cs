using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Attachment;

public class AttachmentUploadViewModel
{
    [Required] public int ResourceId { get; set; }
    [Required] public IFormFile File { get; set; } = null!;
}