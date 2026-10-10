using Amazon.Runtime;
using Amazon.S3;
using Heimevernet.Infrastructure.Storage;
using Heimevernet.Mappers;
using Heimevernet.Repositories;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<INeedRepository, NeedRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }

    public static IServiceCollection AddFileStorage(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .Validate(o =>
                !string.IsNullOrWhiteSpace(o.Endpoint) &&
                !string.IsNullOrWhiteSpace(o.Region) &&
                !string.IsNullOrWhiteSpace(o.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(o.SecretAccessKey) &&
                !string.IsNullOrWhiteSpace(o.Bucket) &&
                !string.IsNullOrWhiteSpace(o.PublicBaseUrl),
                "Storage configuration is incomplete.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return new AmazonS3Client(
                new BasicAWSCredentials(o.AccessKeyId, o.SecretAccessKey),
                new AmazonS3Config
                {
                    ServiceURL = o.Endpoint,
                    AuthenticationRegion = o.Region,
                    ForcePathStyle = true
                });
        });
        services.AddSingleton<IFileStorage, S3FileStorage>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<INeedService, NeedService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddSingleton<INeedMapper, NeedMapper>();
        services.AddSingleton<IResourceMapper, ResourceMapper>();
        return services;
    }
}