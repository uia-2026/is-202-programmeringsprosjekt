using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimevernet.Data;

public class AppDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
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