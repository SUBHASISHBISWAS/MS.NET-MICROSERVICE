namespace BuildingBlocks.EventSourcing;

/// <summary>
/// Repository for event-sourced aggregates
/// </summary>
public interface IEventSourcedRepository<T> where T : IEventSourcedAggregate
{
    /// <summary>
    /// Loads an aggregate from the event store
    /// </summary>
    Task<T?> GetByIdAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an aggregate to the event store
    /// </summary>
    Task SaveAsync(T aggregate, CancellationToken cancellationToken = default);
}
