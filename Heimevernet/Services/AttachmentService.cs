using Heimevernet.Infrastructure.Storage;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Attachment;

namespace Heimevernet.Services;

public sealed class AttachmentService : IAttachmentService
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    private readonly IAttachmentRepository _attachments;
    private readonly IResourceRepository _resources;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(
        IAttachmentRepository attachments,
        IResourceRepository resources,
        IFileStorage storage,
        IUnitOfWork uow,
        ILogger<AttachmentService> logger)
    {
        _attachments = attachments;
        _resources = resources;
        _storage = storage;
        _uow = uow;
        _logger = logger;
    }

    public async Task<AttachmentResult> UploadAsync(
        int resourceId, Stream content, string fileName, long length, int userId, CancellationToken ct = default)
    {
        if (length <= 0 || length > MaxFileSize)
            return new(false, "Image must be between 1 byte and 5 MB.");

        var extension = Path.GetExtension(fileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
            return new(false, "Only JPEG, PNG and WebP images are allowed.");

        if (!content.CanSeek || !FileValidation.IsValidImage(content))
            return new(false, "The uploaded file is not a supported image.");

        var resource = await _resources.GetByIdAsync(resourceId, ct);
        if (resource is null || resource.UserId != userId)
            return new(false, "Resource not found.");

        var key = $"resources/{resourceId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        try
        {
            await _storage.UploadAsync(key, content, contentType, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Storage upload failed for resource {ResourceId}", resourceId);
            return new(false, "Image upload failed.");
        }

        try
        {
            await _attachments.AddAsync(new Attachment
            {
                ResourceId = resourceId,
                StorageKey = key,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            }, ct);
            await _uow.CommitAsync();
        }
        catch
        {
            await TryDeleteFileAsync(key); // compensate so no orphan file is left behind
            throw;
        }

        return new(true, null);
    }

    public async Task<AttachmentResult> DeleteAsync(int attachmentId, int userId, CancellationToken ct = default)
    {
        var attachment = await _attachments.GetByIdAsync(attachmentId, ct);
        if (attachment is null || attachment.UploadedByUserId != userId)
            return new(false, "Attachment not found.");

        _attachments.Remove(attachment);
        await _uow.CommitAsync();
        await TryDeleteFileAsync(attachment.StorageKey);

        return new(true, null);
    }

    public async Task<IReadOnlyList<AttachmentViewModel>> GetForResourceAsync(int resourceId, CancellationToken ct = default)
    {
        var items = await _attachments.GetByResourceIdAsync(resourceId, ct);

        return items
            .Select(a => new AttachmentViewModel { Id = a.Id, PublicUrl = _storage.GetPublicUrl(a.StorageKey) })
            .ToList();
    }

    private async Task TryDeleteFileAsync(string key)
    {
        try
        {
            await _storage.DeleteAsync(key, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete storage object {Key}; it is now orphaned.", key);
        }
    }
}