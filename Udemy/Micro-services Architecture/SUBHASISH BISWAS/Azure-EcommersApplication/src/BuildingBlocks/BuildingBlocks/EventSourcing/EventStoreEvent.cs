namespace BuildingBlocks.EventSourcing;

/// <summary>
/// Wrapper for events stored in the event store with metadata
/// </summary>
public class EventStoreEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string EventData { get; set; } = default!;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // CosmosDB partition key
    public string PartitionKey => AggregateId.ToString();
}
