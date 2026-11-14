namespace Heimevernet.Infrastructure.Storage;

/// <summary>
/// Settings for S3-compatible object storage, read from the <c>Storage</c> configuration section.
/// All values are required and checked at startup.
/// </summary>
public class StorageOptions
{
    /// <summary>Name of the configuration section the options are read from.</summary>
    public const string SectionName = "Storage";

    /// <summary>URL of the S3 service.</summary>
    public string Endpoint { get; set; } = "";
    /// <summary>Region name used when signing requests.</summary>
    public string Region { get; set; } = "";
    /// <summary>Access key ID. Keep it in user secrets or environment variables, not in source control.</summary>
    public string AccessKeyId { get; set; } = "";
    /// <summary>Secret access key. Keep it in user secrets or environment variables, not in source control.</summary>
    public string SecretAccessKey { get; set; } = "";
    /// <summary>Name of the bucket the files are stored in.</summary>
    public string Bucket { get; set; } = "";
    /// <summary>Public base URL used to build image links.</summary>
    public string PublicBaseUrl { get; set; } = "";
}