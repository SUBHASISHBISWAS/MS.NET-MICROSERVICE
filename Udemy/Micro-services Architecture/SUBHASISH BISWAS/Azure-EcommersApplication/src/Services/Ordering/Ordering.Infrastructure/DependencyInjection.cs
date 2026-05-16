using BuildingBlocks.EventSourcing;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ordering.Application.Data;
using Ordering.Infrastructure.EventStore;

namespace Ordering.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices
        (this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        // Add services to the container.
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        return services;
    }

    public static IServiceCollection AddEventSourcingServices
        (this IServiceCollection services, IConfiguration configuration)
    {
        // CosmosDB Configuration
        var cosmosDbEndpoint = configuration["CosmosDb:Endpoint"];
        var cosmosDbKey = configuration["CosmosDb:Key"];
        var cosmosDbDatabaseName = configuration["CosmosDb:DatabaseName"];
        var cosmosDbContainerName = configuration["CosmosDb:ContainerName"];

        if (string.IsNullOrEmpty(cosmosDbEndpoint) || string.IsNullOrEmpty(cosmosDbKey))
        {
            throw new InvalidOperationException("CosmosDB configuration is missing. Please configure CosmosDb:Endpoint and CosmosDb:Key in appsettings.json");
        }

        // Register CosmosClient as singleton
        services.AddSingleton(sp =>
        {
            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };
            return new CosmosClient(cosmosDbEndpoint, cosmosDbKey, clientOptions);
        });

        // Register Event Store
        services.AddScoped<IEventStore>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<ILogger<CosmosDbEventStore>>();
            return new CosmosDbEventStore(cosmosClient, cosmosDbDatabaseName!, cosmosDbContainerName!, logger);
        });

        // Register Event-Sourced Repository
        services.AddScoped(typeof(IEventSourcedRepository<>), typeof(EventSourcedRepository<>));

        return services;
    }
}
