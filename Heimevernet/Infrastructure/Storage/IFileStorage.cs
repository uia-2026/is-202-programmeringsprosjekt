namespace Heimevernet.Infrastructure.Storage;

/// <summary>
/// Stores files (resource photos) in object storage.
/// </summary>
public interface IFileStorage
{
    /// <summary>Uploads a file and returns its storage key.</summary>
    /// <param name="key">Storage key (path) for the file.</param>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">MIME type, e.g. "image/png".</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The storage key of the uploaded file.</returns>
    Task<string> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    /// <summary>Deletes an object by its storage key.</summary>
    /// <param name="key">Storage key of the file.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the file is deleted.</returns>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Builds the public URL for a stored file. Does not check that the file exists.
    /// </summary>
    /// <param name="key">Storage key of the file.</param>
    /// <returns>The absolute URL the browser can load the file from.</returns>
    string GetPublicUrl(string key);
}