using Heimevernet.ViewModels.Attachment;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Outcome of an attachment upload or delete.
/// </summary>
/// <param name="Success"><see langword="true"/> when the operation completed.</param>
/// <param name="Error">A user-facing error message when <paramref name="Success"/> is <see langword="false"/>; otherwise <see langword="null"/>.</param>
public record AttachmentResult(bool Success, string? Error);

/// <summary>
/// Handles image attachments (photos) on resources: upload, delete and listing.
/// Files are stored through <see cref="Heimevernet.Infrastructure.Storage.IFileStorage"/>.
/// </summary>
public interface IAttachmentService
{
    /// <summary>
    /// Validates and uploads an image for a resource owned by the user, then saves the attachment row.
    /// Only JPEG, PNG and WebP images up to 5 MB are accepted. If the database save fails,
    /// the uploaded file is deleted again so no orphan file is left behind.
    /// </summary>
    /// <param name="resourceId">ID of the resource the image belongs to.</param>
    /// <param name="content">The file content. Must be seekable so the image header can be checked.</param>
    /// <param name="fileName">Original file name, used to find the file extension.</param>
    /// <param name="length">File size in bytes.</param>
    /// <param name="userId">ID of the user uploading. Must own the resource.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An <see cref="AttachmentResult"/> that tells if the upload worked, with an error message if not.</returns>
    Task<AttachmentResult> UploadAsync(int resourceId, Stream content, string fileName, long length, int userId, CancellationToken ct = default);
    /// <summary>
    /// Deletes an attachment uploaded by the user, and then tries to delete the stored file.
    /// </summary>
    /// <param name="attachmentId">ID of the attachment to delete.</param>
    /// <param name="userId">ID of the user deleting. Must be the user who uploaded it.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An <see cref="AttachmentResult"/>; fails with "Attachment not found." if it does not exist or belongs to someone else.</returns>
    Task<AttachmentResult> DeleteAsync(int attachmentId, int userId, CancellationToken ct = default);
    /// <summary>
    /// Lists the attachments for a resource with their public URLs.
    /// </summary>
    /// <param name="resourceId">ID of the resource.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The attachments as <see cref="AttachmentViewModel"/> items. Empty if there are none.</returns>
    Task<IReadOnlyList<AttachmentViewModel>> GetForResourceAsync(int resourceId, CancellationToken ct = default);
    /// <summary>
    /// Deletes stored files by key. Failures are logged and ignored, so this never throws for a missing file.
    /// Used after a resource is deleted to clean up its images.
    /// </summary>
    /// <param name="storageKeys">The storage keys of the files to delete.</param>
    /// <returns>A task that completes when all deletes have been attempted.</returns>
    Task DeleteStorageObjectsAsync(IEnumerable<string> storageKeys);
}