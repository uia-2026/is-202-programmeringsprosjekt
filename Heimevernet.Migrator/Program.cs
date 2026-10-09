using Heimevernet.Data;
using Heimevernet.Infrastructure.Storage;
using Heimevernet.Repositories;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Amazon.Runtime;
using Heimevernet.Mappers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var cs = context.Configuration.GetConnectionString("heimevernetdb")
            ?? context.Configuration["DATABASE_CONNECTION"]
            ?? throw new InvalidOperationException(
                "No connection string configured");

        services.AddDbContext<AppDbContext>(o =>
            o.UseMySql(cs, ServerVersion.AutoDetect(cs)));

        // Application services
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<INeedService, NeedService>();
        services.AddScoped<IUserService, UserService>();

        // Repositories and unit of work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<INeedRepository, NeedRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();


        services.AddSingleton<INeedMapper, NeedMapper>();
        services.AddSingleton<IResourceMapper, ResourceMapper>();

        services
            .AddOptions<StorageOptions>()
            .BindConfiguration("Storage")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Endpoint) &&
                    !string.IsNullOrWhiteSpace(options.AccessKeyId) &&
                    !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
                    !string.IsNullOrWhiteSpace(options.Bucket),
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
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        // File storage
        services.AddSingleton<IFileStorage, S3FileStorage>();

        services.AddOptions<StorageOptions>()
    .BindConfiguration(StorageOptions.SectionName)
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Endpoint) &&
            !string.IsNullOrWhiteSpace(options.AccessKeyId) &&
            !string.IsNullOrWhiteSpace(options.SecretAccessKey) &&
            !string.IsNullOrWhiteSpace(options.Bucket),
        "Storage configuration is incomplete.")
    .ValidateOnStart();
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
    Console.WriteLine("Database migrated successfully");

    var environment =
        scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

    Console.WriteLine(
        $"Migrator environment: {environment.EnvironmentName}");


    var attachmentService =
        scope.ServiceProvider.GetRequiredService<IAttachmentService>();

    await SeedData.SeedAsync(
        db,
        attachmentService,
        environment,
        CancellationToken.None);

    Console.WriteLine("Development seed data inserted");

}