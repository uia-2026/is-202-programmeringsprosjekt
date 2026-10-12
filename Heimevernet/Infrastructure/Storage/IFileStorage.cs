namespace Heimevernet.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Uploads a file and returns its storage key.</summary>
    Task<string> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default);

    /// <summary>Deletes an object by its storage key.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    string GetPublicUrl(string key);
}

