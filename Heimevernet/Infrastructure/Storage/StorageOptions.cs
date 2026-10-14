namespace Heimevernet.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; set; } = "";
    public string Region { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
}