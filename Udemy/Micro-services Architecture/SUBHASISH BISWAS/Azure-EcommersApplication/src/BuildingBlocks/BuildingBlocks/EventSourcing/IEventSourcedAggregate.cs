namespace BuildingBlocks.EventSourcing;

/// <summary>
/// Interface for aggregates that support event sourcing
/// </summary>
public interface IEventSourcedAggregate
{
    Guid Id { get; }
    int Version { get; }

    /// <summary>
    /// Gets uncommitted events that have been applied to the aggregate
    /// </summary>
    IEnumerable<object> GetUncommittedEvents();

    /// <summary>
    /// Marks all events as committed
    /// </summary>
    void MarkEventsAsCommitted();

    /// <summary>
    /// Loads the aggregate state from historical events
    /// </summary>
    void LoadFromHistory(IEnumerable<object> events);
}
