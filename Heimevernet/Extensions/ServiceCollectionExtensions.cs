using Amazon.Runtime;
using Amazon.S3;
using Heimevernet.Infrastructure.Storage;
using Heimevernet.Mappers;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Repositories;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Extensions;

/// <summary>
/// Startup registration for repositories, file storage and application services.
/// Called from <c>Program.cs</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the unit of work and all repositories as scoped services.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, so calls can be chained.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<INeedRepository, NeedRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }

    /// <summary>
    /// Registers S3-compatible file storage. The <c>Storage</c> settings are validated at startup.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, so calls can be chained.</returns>
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

    /// <summary>
    /// Registers the application services, mappers, the current-user accessor and the Nominatim HTTP client
    /// used for reverse-geocoding.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, so calls can be chained.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<INeedService, NeedService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IYourDataService, YourDataService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IMapService, MapService>();
        services.AddScoped<ISearchService, SearchService>();

        // Region resolver and HTTP client used for reverse geocoding
        services.AddHttpClient("nominatim", client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Heimevernet/1.0 (contact@heimevernet.example)");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped<IRegionResolver, RegionResolver>();

        services.AddSingleton<INeedMapper, NeedMapper>();
        services.AddSingleton<IResourceMapper, ResourceMapper>();
        services.AddSingleton<IMapMarkerMapper, MapMarkerMapper>();
        services.AddSingleton<INearbyResourceMapper, NearbyResourceMapper>();
        services.AddScoped<IMatchCandidateMapper, MatchCandidateMapper>();
        services.AddSingleton<IMatchListMapper, MatchListMapper>(); // NYTT I STEG 5

        return services;
    }
}