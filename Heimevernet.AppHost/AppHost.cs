var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Heimevernet>("heimevernet");

builder.Build().Run();
