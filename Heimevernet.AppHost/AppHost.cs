var builder = DistributedApplication.CreateBuilder(args);

var mariadbPassword = builder.AddParameter("mariadb-password", secret: true);

var mariadb = builder.AddMySql("mariadb", password: mariadbPassword, port: 3307)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithImage("mariadb", "11")
    .AddDatabase("heimevernetdb");

var migrator = builder.AddProject<Projects.Heimevernet_Migrator>("migrator")
    .WithReference(mariadb)
    .WaitFor(mariadb);

builder.AddProject<Projects.Heimevernet>("heimevernet")
    .WithReference(mariadb)
    .WaitFor(mariadb)
    .WaitForCompletion(migrator);

builder.Build().Run();
