var builder = DistributedApplication.CreateBuilder(args);



// Backing Services
var postgres = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalogDb = postgres.AddDatabase("catalogdb");

var cache = builder
    .AddRedis("cache")
    .WithRedisInsight()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var catalog=builder.AddProject<Projects.Catalog>("catalog").WithReference(catalogDb).WithReference(rabbitmq).WaitFor
    (catalogDb).WaitFor(rabbitmq);

var basket=builder.AddProject<Projects.Basket>("basket").WithReference(cache).WithReference(catalog).WithReference(rabbitmq)
    .WaitFor
    (cache).WaitFor(catalog).WaitFor(rabbitmq);

builder.Build().Run();