using BuildingBlocks.EventSourcing;

namespace Ordering.Domain.Models;

/// <summary>
/// Event-Sourced Order Aggregate
/// This aggregate rebuilds its state from a stream of events stored in CosmosDB
/// </summary>
public class OrderES : EventSourcedAggregate
{
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public CustomerId CustomerId { get; private set; } = default!;
    public OrderName OrderName { get; private set; } = default!;
    public Address ShippingAddress { get; private set; } = default!;
    public Address BillingAddress { get; private set; } = default!;
    public Payment Payment { get; private set; } = default!;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalPrice => OrderItems.Sum(x => x.Price * x.Quantity);

    // Parameterless constructor required for event sourcing
    private OrderES() { }

    /// <summary>
    /// Factory method to create a new order
    /// </summary>
    public static OrderES Create(
        OrderId id,
        CustomerId customerId,
        OrderName orderName,
        Address shippingAddress,
        Address billingAddress,
        Payment payment)
    {
        var order = new OrderES();

        var @event = new OrderCreatedEventES(
            id.Value,
            customerId.Value,
            orderName.Value,
            shippingAddress.FirstName,
            shippingAddress.LastName,
            shippingAddress.EmailAddress,
            shippingAddress.AddressLine,
            shippingAddress.Country,
            shippingAddress.State,
            shippingAddress.ZipCode,
            billingAddress.FirstName,
            billingAddress.LastName,
            billingAddress.EmailAddress,
            billingAddress.AddressLine,
            billingAddress.Country,
            billingAddress.State,
            billingAddress.ZipCode,
            payment.CardName,
            payment.CardNumber,
            payment.Expiration,
            payment.CVV,
            payment.PaymentMethod
        );

        order.ApplyEvent(@event);
        return order;
    }

    /// <summary>
    /// Updates the order
    /// </summary>
    public void Update(OrderName orderName, Address shippingAddress, Address billingAddress, Payment payment, OrderStatus status)
    {
        var @event = new OrderUpdatedEventES(
            Id,
            orderName.Value,
            shippingAddress.FirstName,
            shippingAddress.LastName,
            shippingAddress.EmailAddress,
            shippingAddress.AddressLine,
            shippingAddress.Country,
            shippingAddress.State,
            shippingAddress.ZipCode,
            billingAddress.FirstName,
            billingAddress.LastName,
            billingAddress.EmailAddress,
            billingAddress.AddressLine,
            billingAddress.Country,
            billingAddress.State,
            billingAddress.ZipCode,
            payment.CardName,
            payment.CardNumber,
            payment.Expiration,
            payment.CVV,
            payment.PaymentMethod,
            status
        );

        ApplyEvent(@event);
    }

    /// <summary>
    /// Adds an item to the order
    /// </summary>
    public void AddItem(ProductId productId, int quantity, decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        var @event = new OrderItemAddedEvent(Id, productId.Value, quantity, price);
        ApplyEvent(@event);
    }

    /// <summary>
    /// Removes an item from the order
    /// </summary>
    public void RemoveItem(ProductId productId)
    {
        var orderItem = _orderItems.FirstOrDefault(x => x.ProductId == productId);
        if (orderItem is null)
        {
            throw new InvalidOperationException($"Product {productId.Value} not found in order");
        }

        var @event = new OrderItemRemovedEvent(Id, productId.Value);
        ApplyEvent(@event);
    }

    // ============ Event Apply Methods (State Mutation) ============

    /// <summary>
    /// Applies OrderCreatedEventES to rebuild state
    /// </summary>
    private void Apply(OrderCreatedEventES @event)
    {
        Id = @event.OrderId;
        CustomerId = CustomerId.Of(@event.CustomerId);
        OrderName = OrderName.Of(@event.OrderName);
        ShippingAddress = Address.Of(
            @event.ShippingFirstName,
            @event.ShippingLastName,
            @event.ShippingEmailAddress,
            @event.ShippingAddressLine,
            @event.ShippingCountry,
            @event.ShippingState,
            @event.ShippingZipCode);
        BillingAddress = Address.Of(
            @event.BillingFirstName,
            @event.BillingLastName,
            @event.BillingEmailAddress,
            @event.BillingAddressLine,
            @event.BillingCountry,
            @event.BillingState,
            @event.BillingZipCode);
        Payment = Payment.Of(
            @event.CardName,
            @event.CardNumber,
            @event.Expiration,
            @event.CVV,
            @event.PaymentMethod);
        Status = OrderStatus.Pending;
    }

    /// <summary>
    /// Applies OrderUpdatedEventES to rebuild state
    /// </summary>
    private void Apply(OrderUpdatedEventES @event)
    {
        OrderName = OrderName.Of(@event.OrderName);
        ShippingAddress = Address.Of(
            @event.ShippingFirstName,
            @event.ShippingLastName,
            @event.ShippingEmailAddress,
            @event.ShippingAddressLine,
            @event.ShippingCountry,
            @event.ShippingState,
            @event.ShippingZipCode);
        BillingAddress = Address.Of(
            @event.BillingFirstName,
            @event.BillingLastName,
            @event.BillingEmailAddress,
            @event.BillingAddressLine,
            @event.BillingCountry,
            @event.BillingState,
            @event.BillingZipCode);
        Payment = Payment.Of(
            @event.CardName,
            @event.CardNumber,
            @event.Expiration,
            @event.CVV,
            @event.PaymentMethod);
        Status = @event.Status;
    }

    /// <summary>
    /// Applies OrderItemAddedEvent to rebuild state
    /// </summary>
    private void Apply(OrderItemAddedEvent @event)
    {
        var orderItem = new OrderItem(
            OrderId.Of(@event.OrderId),
            ProductId.Of(@event.ProductId),
            @event.Quantity,
            @event.Price);
        _orderItems.Add(orderItem);
    }

    /// <summary>
    /// Applies OrderItemRemovedEvent to rebuild state
    /// </summary>
    private void Apply(OrderItemRemovedEvent @event)
    {
        var orderItem = _orderItems.FirstOrDefault(x => x.ProductId.Value == @event.ProductId);
        if (orderItem is not null)
        {
            _orderItems.Remove(orderItem);
        }
    }
}
