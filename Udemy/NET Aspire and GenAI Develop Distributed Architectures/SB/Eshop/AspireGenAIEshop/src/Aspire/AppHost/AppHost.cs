var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Catalog>("catalog");
//backing services

// Backing Services
var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var catalog=builder.AddProject<Projects.Catalog>("catalog").WithReference(catalogDb).WaitFor(catalogDb);

builder.Build().Run();