using System.ComponentModel.DataAnnotations;

public class AttachmentUploadViewModel
{
    [Required] public int ResourceId { get; set; }
    [Required] public IFormFile File { get; set; } = null!;
}