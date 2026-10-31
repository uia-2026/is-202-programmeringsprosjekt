extern alias WebApp;

using Heimevernet.Data;
using Heimevernet.Migrator;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MariaDb;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Custom ASP.NET Core <see cref="WebApplicationFactory{TEntryPoint}"/> that spins up an isolated 
/// MariaDB Testcontainer and configures test application services and environment settings.
/// </summary>
public sealed class HeimevernetWebApplicationFactory
    : WebApplicationFactory<WebApp::Program>, IAsyncLifetime
{
    // Configure an isolated MariaDB docker container for integration testing.
    private readonly MariaDbContainer _database =
        new MariaDbBuilder("mariadb:11")
            .WithDatabase("heimevernet_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

    /// <summary>
    /// Initializes the test environment by starting the MariaDB container 
    /// and running EF Core database migrations before any tests execute.
    /// </summary>
    public async Task InitializeAsync()
    {
        // 1. Start MariaDB container first.
        await _database.StartAsync();

        // 2. Trigger host creation and apply database migrations AFTER container is running.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        await SeedData.SeedReferenceDataAsync(db, CancellationToken.None);
    }

    /// <summary>
    /// Overrides web host configuration to set the test environment, dynamic connection strings, 
    /// user secrets, and cookie policy overrides for test clients.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set ASPNETCORE_ENVIRONMENT to "Testing".
        builder.UseEnvironment("Testing");

        // Inject dynamic container connection settings before the host boots.
        builder.UseSetting("ConnectionStrings:heimevernetdb", _database.GetConnectionString());
        builder.UseSetting("Storage:Bucket", "test_bucket_is202");
        builder.UseSetting("Storage:PublicBaseUrl", "https://hhbpqeqznbrwzuooqakk.supabase.co/storage/v1/object/public/test_bucket_is202");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            // Load user secrets from the main project assembly if present.
            configuration.AddUserSecrets<WebApp::Program>(optional: true);
        });

        builder.ConfigureServices(services =>
        {
            // Ensure cookie authentication scheme accepts HTTP cookies issued by WebApplicationFactory test client.
            services.PostConfigure<CookieAuthenticationOptions>(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                });
        });
    }

    /// <summary>
    /// Cleans up resources asynchronously when the test fixture is disposed.
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
    }
}