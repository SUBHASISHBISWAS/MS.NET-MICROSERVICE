using BuildingBlocks.EventSourcing;

namespace Ordering.Application.Orders.Commands.UpdateOrder;

/// <summary>
/// Event-Sourced version of UpdateOrderHandler
/// Uses CosmosDB Event Store instead of EF Core
/// </summary>
public class UpdateOrderHandlerES(IEventSourcedRepository<OrderES> repository)
    : ICommandHandler<UpdateOrderCommand, UpdateOrderResult>
{
    public async Task<UpdateOrderResult> Handle(UpdateOrderCommand command, CancellationToken cancellationToken)
    {
        // Load order from event store
        var order = await repository.GetByIdAsync(command.Order.Id, cancellationToken);

        if (order is null)
        {
            throw new OrderNotFoundException(command.Order.Id);
        }

        // Update order with new values
        UpdateOrderWithNewValues(order, command.Order);

        // Save updated order to event store
        await repository.SaveAsync(order, cancellationToken);

        return new UpdateOrderResult(true);
    }

    private void UpdateOrderWithNewValues(OrderES order, OrderDto orderDto)
    {
        var updatedShippingAddress = Address.Of(
            orderDto.ShippingAddress.FirstName,
            orderDto.ShippingAddress.LastName,
            orderDto.ShippingAddress.EmailAddress,
            orderDto.ShippingAddress.AddressLine,
            orderDto.ShippingAddress.Country,
            orderDto.ShippingAddress.State,
            orderDto.ShippingAddress.ZipCode);

        var updatedBillingAddress = Address.Of(
            orderDto.BillingAddress.FirstName,
            orderDto.BillingAddress.LastName,
            orderDto.BillingAddress.EmailAddress,
            orderDto.BillingAddress.AddressLine,
            orderDto.BillingAddress.Country,
            orderDto.BillingAddress.State,
            orderDto.BillingAddress.ZipCode);

        var updatedPayment = Payment.Of(
            orderDto.Payment.CardName,
            orderDto.Payment.CardNumber,
            orderDto.Payment.Expiration,
            orderDto.Payment.Cvv,
            orderDto.Payment.PaymentMethod);

        order.Update(
            orderName: OrderName.Of(orderDto.OrderName),
            shippingAddress: updatedShippingAddress,
            billingAddress: updatedBillingAddress,
            payment: updatedPayment,
            status: orderDto.Status);
    }
}
