namespace Ordering.Domain.Events;

/// <summary>
/// Event raised when an item is removed from an order
/// </summary>
public record OrderItemRemovedEvent(
    Guid OrderId,
    Guid ProductId);
