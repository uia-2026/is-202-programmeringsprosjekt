using Heimevernet.ViewModels.Attachment;

namespace Heimevernet.Services.Interfaces;

public record AttachmentResult(bool Success, string? Error);

public interface IAttachmentService
{
    Task<AttachmentResult> UploadAsync(int resourceId, Stream content, string fileName, long length, int userId, CancellationToken ct = default);
    Task<AttachmentResult> DeleteAsync(int attachmentId, int userId, CancellationToken ct = default);
    Task<IReadOnlyList<AttachmentViewModel>> GetForResourceAsync(int resourceId, CancellationToken ct = default);
}