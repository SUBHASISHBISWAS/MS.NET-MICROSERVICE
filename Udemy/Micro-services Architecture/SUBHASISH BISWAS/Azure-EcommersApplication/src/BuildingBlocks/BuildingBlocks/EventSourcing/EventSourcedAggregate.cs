using System.Reflection;

namespace BuildingBlocks.EventSourcing;

/// <summary>
/// Base class for event-sourced aggregates
/// </summary>
public abstract class EventSourcedAggregate : IEventSourcedAggregate
{
    private readonly List<object> _uncommittedEvents = new();

    public Guid Id { get; protected set; }
    public int Version { get; protected set; } = -1;

    /// <summary>
    /// Gets all uncommitted events
    /// </summary>
    public IEnumerable<object> GetUncommittedEvents()
    {
        return _uncommittedEvents.AsReadOnly();
    }

    /// <summary>
    /// Marks all events as committed by clearing the uncommitted events list
    /// </summary>
    public void MarkEventsAsCommitted()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// Loads aggregate state from historical events
    /// </summary>
    public void LoadFromHistory(IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            ApplyEvent(@event, false);
            Version++;
        }
    }

    /// <summary>
    /// Applies a new event to the aggregate
    /// </summary>
    protected void ApplyEvent(object @event)
    {
        ApplyEvent(@event, true);
    }

    /// <summary>
    /// Applies an event to the aggregate state
    /// </summary>
    private void ApplyEvent(object @event, bool isNew)
    {
        // Use reflection to call the appropriate Apply method
        var eventType = @event.GetType();
        var applyMethod = GetType().GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance, new[] { eventType });

        if (applyMethod == null)
        {
            throw new InvalidOperationException($"Apply method not found for event type {@event.GetType().Name}");
        }

        applyMethod.Invoke(this, new[] { @event });

        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }
}
