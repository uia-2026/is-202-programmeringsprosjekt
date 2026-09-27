using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimevernet.Data;

/// <summary>
/// Design-time factory used by EF Core tools to create AppDbContext instances.
/// Provides a fallback connection string when the environment variable is not set.
/// </summary>
public class AppDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> for design-time tools (migrations, scaffolding).
    /// </summary>
    /// <param name="args">Command-line args supplied by the design-time host.</param>
    /// <returns>A configured <see cref="AppDbContext"/> instance.</returns>
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION")
            ?? "Server=localhost;Port=3307;User ID=root;Password=dev_password_123;Database=heimevernetdb";

        var serverVersion = new MariaDbServerVersion(new Version(11, 0, 0));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connection, serverVersion)
            .Options;

        return new AppDbContext(options);
    }
}