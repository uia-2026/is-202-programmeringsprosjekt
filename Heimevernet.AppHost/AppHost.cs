var builder = DistributedApplication.CreateBuilder(args);

var mariadb = builder.AddMySql("mariadb")
    .WithImage("mariadb", "11")
    .WithDataVolume()
    .AddDatabase("heimevernetdb");

var migrator = builder.AddProject<Projects.Heimevernet_Migrator>("migrator")
    .WithReference(mariadb)
    .WaitFor(mariadb);

builder.AddProject<Projects.Heimevernet>("heimevernet")
    .WithReference(mariadb)
    .WaitFor(mariadb)
    .WaitForCompletion(migrator);

builder.Build().Run();
