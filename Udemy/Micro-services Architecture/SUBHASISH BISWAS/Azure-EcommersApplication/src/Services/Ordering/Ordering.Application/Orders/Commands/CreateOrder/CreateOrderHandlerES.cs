using BuildingBlocks.EventSourcing;

namespace Ordering.Application.Orders.Commands.CreateOrder;

/// <summary>
/// Event-Sourced version of CreateOrderHandler
/// Uses CosmosDB Event Store instead of EF Core
/// </summary>
public class CreateOrderHandlerES(IEventSourcedRepository<OrderES> repository)
    : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // Create Order entity from command object using event sourcing
        var order = CreateNewOrder(command.Order);

        // Add order items
        foreach (var orderItemDto in command.Order.OrderItems)
        {
            order.AddItem(ProductId.Of(orderItemDto.ProductId), orderItemDto.Quantity, orderItemDto.Price);
        }

        // Save to event store
        await repository.SaveAsync(order, cancellationToken);

        return new CreateOrderResult(order.Id);
    }

    private OrderES CreateNewOrder(OrderDto orderDto)
    {
        var shippingAddress = Address.Of(
            orderDto.ShippingAddress.FirstName,
            orderDto.ShippingAddress.LastName,
            orderDto.ShippingAddress.EmailAddress,
            orderDto.ShippingAddress.AddressLine,
            orderDto.ShippingAddress.Country,
            orderDto.ShippingAddress.State,
            orderDto.ShippingAddress.ZipCode);

        var billingAddress = Address.Of(
            orderDto.BillingAddress.FirstName,
            orderDto.BillingAddress.LastName,
            orderDto.BillingAddress.EmailAddress,
            orderDto.BillingAddress.AddressLine,
            orderDto.BillingAddress.Country,
            orderDto.BillingAddress.State,
            orderDto.BillingAddress.ZipCode);

        var newOrder = OrderES.Create(
            id: OrderId.Of(Guid.NewGuid()),
            customerId: CustomerId.Of(orderDto.CustomerId),
            orderName: OrderName.Of(orderDto.OrderName),
            shippingAddress: shippingAddress,
            billingAddress: billingAddress,
            payment: Payment.Of(
                orderDto.Payment.CardName,
                orderDto.Payment.CardNumber,
                orderDto.Payment.Expiration,
                orderDto.Payment.Cvv,
                orderDto.Payment.PaymentMethod)
        );

        return newOrder;
    }
}
