using BuildingBlocks.EventSourcing;
using Microsoft.Extensions.Logging;

namespace Ordering.Infrastructure.EventStore;

/// <summary>
/// Generic repository implementation for event-sourced aggregates
/// </summary>
public class EventSourcedRepository<T> : IEventSourcedRepository<T> where T : IEventSourcedAggregate
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<EventSourcedRepository<T>> _logger;

    public EventSourcedRepository(IEventStore eventStore, ILogger<EventSourcedRepository<T>> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading aggregate {AggregateType} with ID {AggregateId}", typeof(T).Name, aggregateId);

        var events = await _eventStore.GetEventsAsync(aggregateId, cancellationToken);
        var eventsList = events.ToList();

        if (!eventsList.Any())
        {
            _logger.LogWarning("No events found for aggregate {AggregateId}", aggregateId);
            return default;
        }

        // Create aggregate instance using reflection
        var aggregate = (T)Activator.CreateInstance(typeof(T), true)!;
        aggregate.LoadFromHistory(eventsList);

        _logger.LogInformation("Loaded aggregate {AggregateType} with ID {AggregateId}, version {Version}",
            typeof(T).Name, aggregateId, aggregate.Version);

        return aggregate;
    }

    public async Task SaveAsync(T aggregate, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving aggregate {AggregateType} with ID {AggregateId}", typeof(T).Name, aggregate.Id);

        var uncommittedEvents = aggregate.GetUncommittedEvents().ToList();
        if (!uncommittedEvents.Any())
        {
            _logger.LogInformation("No uncommitted events for aggregate {AggregateId}", aggregate.Id);
            return;
        }

        await _eventStore.SaveEventsAsync<T>(aggregate.Id, uncommittedEvents, aggregate.Version, cancellationToken);
        aggregate.MarkEventsAsCommitted();

        _logger.LogInformation("Saved {EventCount} events for aggregate {AggregateId}", uncommittedEvents.Count, aggregate.Id);
    }
}
