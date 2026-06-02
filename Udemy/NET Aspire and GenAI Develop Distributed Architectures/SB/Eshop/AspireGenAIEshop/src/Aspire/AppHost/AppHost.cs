var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Catalog>("catalog");

builder.Build().Run();