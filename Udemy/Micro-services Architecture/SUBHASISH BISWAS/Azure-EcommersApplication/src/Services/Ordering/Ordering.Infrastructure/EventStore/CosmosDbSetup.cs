using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ordering.Infrastructure.EventStore;

/// <summary>
/// Helper class to initialize CosmosDB database and container for event sourcing
/// </summary>
public class CosmosDbSetup
{
    private readonly CosmosClient _cosmosClient;
    private readonly string _databaseName;
    private readonly string _containerName;
    private readonly ILogger<CosmosDbSetup> _logger;

    public CosmosDbSetup(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CosmosDbSetup> logger)
    {
        _cosmosClient = cosmosClient;
        _databaseName = configuration["CosmosDb:DatabaseName"]!;
        _containerName = configuration["CosmosDb:ContainerName"]!;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the CosmosDB database and container if they don't exist
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing CosmosDB Event Store...");

            // Create database if it doesn't exist
            var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(
                _databaseName,
                throughput: 400 // Shared throughput (cheaper option)
            );

            if (databaseResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Created CosmosDB database: {DatabaseName}", _databaseName);
            }
            else
            {
                _logger.LogInformation("CosmosDB database already exists: {DatabaseName}", _databaseName);
            }

            var database = _cosmosClient.GetDatabase(_databaseName);

            // Create container if it doesn't exist
            // Partition key is AggregateId for efficient querying
            var containerProperties = new ContainerProperties
            {
                Id = _containerName,
                PartitionKeyPath = "/partitionKey", // Uses the PartitionKey property from EventStoreEvent
                DefaultTimeToLive = -1 // No TTL, events are kept forever
            };

            var containerResponse = await database.CreateContainerIfNotExistsAsync(
                containerProperties
            );

            if (containerResponse.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Created CosmosDB container: {ContainerName}", _containerName);
            }
            else
            {
                _logger.LogInformation("CosmosDB container already exists: {ContainerName}", _containerName);
            }

            _logger.LogInformation("CosmosDB Event Store initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing CosmosDB Event Store");
            throw;
        }
    }
}
