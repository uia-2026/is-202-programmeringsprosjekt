using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Storage;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _amazonS3;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public S3FileStorage(
        IAmazonS3 amazonS3,
        IOptions<StorageOptions> options)
    {
        _amazonS3 = amazonS3;

        var storage = options.Value;

        _bucketName = !string.IsNullOrWhiteSpace(storage.Bucket)
            ? storage.Bucket
            : throw new InvalidOperationException(
                "Storage:Bucket is not configured.");

        _publicBaseUrl = !string.IsNullOrWhiteSpace(storage.PublicBaseUrl)
            ? storage.PublicBaseUrl.TrimEnd('/')
            : throw new InvalidOperationException("Storage:PublicBaseUrl is not configured.");
    }

    public async Task<string> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType
        };

        await _amazonS3.PutObjectAsync(request, ct);

        return key;
    }

    public async Task DeleteAsync(
        string key,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _amazonS3.DeleteObjectAsync(request, ct);
    }

    public string GetPublicUrl(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return $"{_publicBaseUrl}/{key}";
    }
}

