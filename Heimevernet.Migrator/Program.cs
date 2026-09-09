using Heimevernet.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var cs = context.Configuration.GetConnectionString("heimevernetdb")
            ?? context.Configuration["DATABASE_CONNECTION"]
            ?? throw new InvalidOperationException("No connection string configured");

        services.AddDbContext<AppDbContext>(o =>
            o.UseMySql(cs, ServerVersion.AutoDetect(cs)));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
    Console.WriteLine("✅ Database migrated successfully");

    var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (environment.IsDevelopment() && configuration["SeedData:Enabled"] != "false")
    {
        await SeedData.SeedAsync(db, CancellationToken.None);
        Console.WriteLine("✅ Dev seed data inserted");
    }
}