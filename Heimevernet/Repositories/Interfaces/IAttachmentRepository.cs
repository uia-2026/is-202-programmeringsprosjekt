using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access for <see cref="Attachment"/> rows (images linked to resources).
/// </summary>
public interface IAttachmentRepository
{
    /// <summary>
    /// Adds an attachment to the context. It is saved on the next unit of work commit.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the attachment has been added to the context.</returns>
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    /// <summary>
    /// Finds an attachment by ID.
    /// </summary>
    /// <param name="id">ID of the attachment.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The attachment, or <see langword="null"/> if it does not exist.</returns>
    Task<Attachment?> GetByIdAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Lists the attachments for a resource.
    /// </summary>
    /// <param name="resourceId">ID of the resource.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The attachments, or an empty list.</returns>
    Task<IReadOnlyList<Attachment>> GetByResourceIdAsync(int resourceId, CancellationToken ct = default);
    /// <summary>
    /// Marks an attachment for deletion. It is deleted on the next unit of work commit.
    /// </summary>
    /// <param name="attachment">The attachment to remove.</param>
    void Remove(Attachment attachment);
}