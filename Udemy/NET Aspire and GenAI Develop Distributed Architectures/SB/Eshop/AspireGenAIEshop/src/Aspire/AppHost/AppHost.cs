var builder = DistributedApplication.CreateBuilder(args);



// Backing Services
var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var catalog=builder.AddProject<Projects.Catalog>("catalog").WithReference(catalogDb).WaitFor(catalogDb);

var basket=builder.AddProject<Projects.Basket>("basket");

builder.Build().Run();