// Lightweight application host that composes the distributed application used for local development.
// It configures dependent services such as MariaDB, Mailpit and the migrator project and then runs the composed app.
var builder = DistributedApplication.CreateBuilder(args);

var mariadbPassword = builder.AddParameter("mariadb-password", secret: true);

var mariadb = builder.AddMySql("mariadb", password: mariadbPassword, port: 3307)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithImage("mariadb", "11")
    .WithDataVolume()
    .AddDatabase("heimevernetdb");

// A local mail server that catches every email in a web inbox, so nothing is really delivered
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")                   // the inbox in the browser
    .WithEndpoint(port: 1025, targetPort: 1025, scheme: "tcp", name: "smtp");     // SMTP, where the app sends mail

var migrator = builder.AddProject<Projects.Heimevernet_Migrator>("migrator")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithReference(mariadb)
    .WaitFor(mariadb);

builder.AddProject<Projects.Heimevernet>("heimevernet")
    .WithReference(mariadb)
    .WaitFor(mariadb)
    .WaitForCompletion(migrator)
    .WaitFor(mailpit)
    .WithEnvironment("Email__Enabled", "true")
    .WithEnvironment("Email__Host", "localhost")
    .WithEnvironment("Email__Port", "1025")
    .WithEnvironment("Email__UseStartTls", "false")
    .WithEnvironment("Email__FromAddress", "noreply@heimevernet.local")
    .WithEnvironment("Notifications__PublicBaseUrl", "https://localhost:7244");

builder.Build().Run();