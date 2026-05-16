namespace Ordering.Domain.Events;

/// <summary>
/// Event raised when an item is added to an order
/// </summary>
public record OrderItemAddedEvent(
    Guid OrderId,
    Guid ProductId,
    int Quantity,
    decimal Price);
