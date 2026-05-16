namespace BuildingBlocks.EventSourcing;

/// <summary>
/// Represents an event store for persisting and retrieving domain events
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Saves events for an aggregate to the event store
    /// </summary>
    Task SaveEventsAsync<T>(Guid aggregateId, IEnumerable<object> events, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all events for an aggregate from the event store
    /// </summary>
    Task<IEnumerable<object>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current version of an aggregate
    /// </summary>
    Task<int> GetVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}
