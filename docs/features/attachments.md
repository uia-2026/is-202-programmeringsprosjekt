# Attachments

Resource providers can add photos to their resources. Photos are optional and never block registering a resource.

## Rules

| Rule    | Value                                                                      |
| ------- | -------------------------------------------------------------------------- |
| Formats | JPEG, PNG and WebP (`.jpg`, `.jpeg`, `.png`, `.webp`)                      |
| Size    | At most 5 MB                                                               |
| Who     | The owner of the resource, and only for their own resources                |
| Where   | Resource details page (upload and remove) and, optionally, the create form |

## Upload flow

`AttachmentService.UploadAsync` runs these checks in order and stops at the first failure:

1. The size is between 1 byte and 5 MB.
2. The extension is on the allowed list, which also determines the stored content type. The browser's `Content-Type` header is never trusted.
3. The file's initial bytes match a supported JPEG, PNG or WebP signature. Renaming a text file to `.jpg` does not pass.
4. The resource exists and belongs to the user. A missing resource and someone else's resource return the same message to avoid revealing ownership.

### 1. Validate the image

The extension determines the content type, while `FileValidation` checks the file signature.

```csharp
if (length <= 0 || length > MaxFileSize)
    return new(false, "Image must be between 1 byte and 5 MB.");

var extension = Path.GetExtension(fileName);

if (!ContentTypes.TryGetValue(extension, out var contentType))
    return new(false, "Only JPEG, PNG and WebP images are allowed.");

if (!content.CanSeek || !FileValidation.IsValidImage(content))
    return new(false, "The uploaded file is not a supported image.");
```

The content type is derived from the extension rather than supplied by the client:

```csharp
private static readonly Dictionary<string, string> ContentTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };
```

`FileValidation.IsValidImage` reads the file signature and resets the stream position before returning, allowing the same stream to be uploaded afterward.

```csharp
stream.Position = 0;

var read = stream.ReadAtLeast(
    header, header.Length, throwOnEndOfStream: false);

stream.Position = 0;

var isJpeg = read >= 3
    && header[0] == 0xFF
    && header[1] == 0xD8
    && header[2] == 0xFF;

var isPng = read >= 8
    && header[..8].SequenceEqual(
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

var isWebp = read >= 12
    && header[..4].SequenceEqual("RIFF"u8)
    && header[8..12].SequenceEqual("WEBP"u8);

return isJpeg || isPng || isWebp;
```

These checks validate the initial signature, not the complete image structure. They do not guarantee that the entire file can be decoded as a valid image.

### 2. Verify ownership

The service checks ownership before uploading anything. Missing resources and resources owned by another user produce the same result.

```csharp
var resource = await _resources.GetByIdAsync(resourceId, ct);

if (resource is null || resource.UserId != userId)
    return new(false, "Resource not found.");
```

### 3. Upload the file

The service generates a random storage key. The original filename is never used as the storage object's name.

```csharp
var key = $"resources/{resourceId}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

await _storage.UploadAsync(key, content, contentType, ct);
```

The key follows this structure:

```text
resources/{resourceId}/{random-guid}.jpg
```

`S3FileStorage` uploads the stream to the configured bucket and sets the content type:

```csharp
var request = new PutObjectRequest
{
    BucketName = _bucketName,
    Key = key,
    InputStream = content,
    ContentType = contentType
};

await _amazonS3.PutObjectAsync(request, ct);
```

Storage errors are logged and returned as a failed upload result, except for cancellation, which is allowed to propagate.

### 4. Save the database record

After the file has been uploaded, the service saves an `Attachment` record containing the storage key and ownership information.

```csharp
await _attachments.AddAsync(new Attachment
{
    ResourceId = resourceId,
    StorageKey = key,
    UploadedByUserId = userId,
    UploadedAt = DateTime.UtcNow
}, ct);

await _uow.CommitAsync();
```

If saving the database record fails, the service attempts to delete the uploaded file before rethrowing the exception.

```csharp
try
{
    await _attachments.AddAsync(attachment, ct);
    await _uow.CommitAsync();
}
catch
{
    await TryDeleteFileAsync(key);
    throw;
}
```

Cleanup is best-effort. If deleting the file also fails, the failure is logged and an unreferenced storage object may remain.

## Delete flow

The service verifies that the attachment belongs to the current user. It then commits the database deletion before attempting to delete the storage object.

```csharp
var attachment = await _attachments.GetByIdAsync(attachmentId, ct);

if (attachment is null || attachment.UploadedByUserId != userId)
    return new(false, "Attachment not found.");

_attachments.Remove(attachment);
await _uow.CommitAsync();

await TryDeleteFileAsync(attachment.StorageKey);

return new(true, null);
```

Deleting the database record first prevents it from pointing to a file that has already been deleted. The trade-off is that a storage deletion failure can leave an unreferenced object.

Storage cleanup is handled separately:

```csharp
private async Task TryDeleteFileAsync(string key)
{
    try
    {
        await _storage.DeleteAsync(key, CancellationToken.None);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(
            ex,
            "Could not delete storage object {Key}; it is now orphaned.",
            key);
    }
}
```

## Storage

| Part           | Detail                                                                                                                                                         |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Interface      | `IFileStorage` provides upload, delete and `GetPublicUrl` operations                                                                                           |
| Implementation | `S3FileStorage` uses the AWS SDK for S3 with a custom endpoint and path-style addressing for S3-compatible providers such as Supabase Storage                  |
| Configuration  | The `Storage` section contains the endpoint, region, access key ID, secret, bucket and public base URL. Configuration is validated when the application starts |
| What is stored | Only the storage key is saved in the database                                                                                                                  |
| Environments   | Development and production use separate buckets selected through configuration                                                                                 |

### Public URL generation

The database stores a key rather than a complete URL. The storage implementation constructs the public URL when a page needs to display an image.

```csharp
public string GetPublicUrl(string key)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    return $"{_publicBaseUrl}/{key}";
}
```

For example, the stored key `resources/12/abc123.jpg` is appended to the configured public base URL.

## Security notes

- The bucket is public. Anyone with an image URL can access the image, so uploaded photos must not contain sensitive information.
- Random GUIDs make storage keys difficult to guess but do not provide authorization or confidentiality.
- The S3 access key can access every bucket in its project. It is stored in user secrets or server-side environment variables and is never sent to the browser.
- Uploads pass through the application server so that size, extension, signature and ownership checks can be applied.
- Signature validation does not guarantee that the complete file is a valid, decodable image. Full image decoding can provide an additional validation step.

## Seed photos

In Development, the Migrator uploads `isbil.jpg` and `drone.jpg` from `SeedAssets` through the same service.

If an image is missing or storage is unavailable, seeding logs a warning and continues. Photos are optional, and their absence must not prevent the application from starting.
