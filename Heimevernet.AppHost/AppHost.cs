var builder = DistributedApplication.CreateBuilder(args);

var mariadb = builder.AddMySql("mariadb")
    .WithImage("mariadb", "11")
    .WithDataVolume()
    .AddDatabase("heimevernetdb");

builder.AddProject<Projects.Heimevernet>("heimevernet")
    .WithReference(mariadb)
    .WaitFor(mariadb);

builder.Build().Run();
