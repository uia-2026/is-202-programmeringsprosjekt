using Heimevernet.Data;
using Heimevernet.Infrastructure.Storage;
using Heimevernet.Migrator;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Heimevernet.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var cs = context.Configuration.GetConnectionString("heimevernetdb")
            ?? context.Configuration["DATABASE_CONNECTION"]
            ?? throw new InvalidOperationException("No connection string configured");

        services.AddDbContext<AppDbContext>(o => o.UseMySql(cs, ServerVersion.AutoDetect(cs)));

        if (context.HostingEnvironment.IsDevelopment())
        {
            services.AddPersistence()
                    .AddFileStorage();
        }
    })
    .Build();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;
var db = sp.GetRequiredService<AppDbContext>();
var env = sp.GetRequiredService<IHostEnvironment>();

await db.Database.MigrateAsync();
Console.WriteLine("Database migrated successfully");
Console.WriteLine($"Migrator environment: {env.EnvironmentName}");

await SeedData.SeedReferenceDataAsync(db, CancellationToken.None);   // categories: all environments

if (env.IsDevelopment())
{
    var attachmentRepository = sp.GetRequiredService<IAttachmentRepository>();
    var fileStorage = sp.GetRequiredService<IFileStorage>();
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    await SeedData.SeedDevDataAsync(
        db, attachmentRepository, fileStorage, unitOfWork, CancellationToken.None);
    Console.WriteLine("Development seed data inserted");
}