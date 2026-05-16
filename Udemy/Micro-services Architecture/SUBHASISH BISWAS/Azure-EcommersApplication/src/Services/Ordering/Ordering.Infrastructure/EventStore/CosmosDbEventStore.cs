using BuildingBlocks.EventSourcing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Ordering.Infrastructure.EventStore;

/// <summary>
/// CosmosDB implementation of the event store
/// </summary>
public class CosmosDbEventStore : IEventStore
{
    private readonly Container _container;
    private readonly ILogger<CosmosDbEventStore> _logger;

    public CosmosDbEventStore(CosmosClient cosmosClient, string databaseName, string containerName, ILogger<CosmosDbEventStore> logger)
    {
        _logger = logger;
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task SaveEventsAsync<T>(Guid aggregateId, IEnumerable<object> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var aggregateType = typeof(T).Name;
        var eventsList = events.ToList();

        if (!eventsList.Any())
        {
            return;
        }

        _logger.LogInformation("Saving {EventCount} events for aggregate {AggregateId}", eventsList.Count, aggregateId);

        // Verify optimistic concurrency
        var currentVersion = await GetVersionAsync(aggregateId, cancellationToken);
        if (currentVersion != expectedVersion)
        {
            throw new InvalidOperationException(
                $"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but current version is {currentVersion}");
        }

        // Save each event
        var version = expectedVersion;
        foreach (var @event in eventsList)
        {
            version++;
            var eventStoreEvent = new EventStoreEvent
            {
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = @event.GetType().AssemblyQualifiedName!,
                EventData = JsonConvert.SerializeObject(@event, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                }),
                Version = version,
                Timestamp = DateTime.UtcNow
            };

            await _container.CreateItemAsync(eventStoreEvent, new PartitionKey(aggregateId.ToString()), cancellationToken: cancellationToken);
            _logger.LogInformation("Saved event {EventType} version {Version} for aggregate {AggregateId}",
                @event.GetType().Name, version, aggregateId);
        }
    }

    public async Task<IEnumerable<object>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading events for aggregate {AggregateId}", aggregateId);

        var query = new QueryDefinition("SELECT * FROM c WHERE c.AggregateId = @aggregateId ORDER BY c.Version")
            .WithParameter("@aggregateId", aggregateId);

        var iterator = _container.GetItemQueryIterator<EventStoreEvent>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(aggregateId.ToString())
        });

        var events = new List<object>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            foreach (var eventStoreEvent in response)
            {
                var eventType = Type.GetType(eventStoreEvent.EventType);
                if (eventType == null)
                {
                    _logger.LogWarning("Could not resolve event type {EventType}", eventStoreEvent.EventType);
                    continue;
                }

                var @event = JsonConvert.DeserializeObject(eventStoreEvent.EventData, eventType, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });

                if (@event != null)
                {
                    events.Add(@event);
                }
            }
        }

        _logger.LogInformation("Loaded {EventCount} events for aggregate {AggregateId}", events.Count, aggregateId);
        return events;
    }

    public async Task<int> GetVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var query = new QueryDefinition("SELECT VALUE MAX(c.Version) FROM c WHERE c.AggregateId = @aggregateId")
            .WithParameter("@aggregateId", aggregateId);

        var iterator = _container.GetItemQueryIterator<int?>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(aggregateId.ToString())
        });

        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            var maxVersion = response.FirstOrDefault();
            return maxVersion ?? -1; // Return -1 if no events exist
        }

        return -1;
    }
}
