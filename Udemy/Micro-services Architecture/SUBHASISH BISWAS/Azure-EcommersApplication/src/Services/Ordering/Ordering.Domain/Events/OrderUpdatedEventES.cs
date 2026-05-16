namespace Ordering.Domain.Events;

/// <summary>
/// Event raised when an order is updated (Event Sourcing version)
/// </summary>
public record OrderUpdatedEventES(
    Guid OrderId,
    string OrderName,
    // Shipping Address
    string ShippingFirstName,
    string ShippingLastName,
    string ShippingEmailAddress,
    string ShippingAddressLine,
    string ShippingCountry,
    string ShippingState,
    string ShippingZipCode,
    // Billing Address
    string BillingFirstName,
    string BillingLastName,
    string BillingEmailAddress,
    string BillingAddressLine,
    string BillingCountry,
    string BillingState,
    string BillingZipCode,
    // Payment
    string CardName,
    string CardNumber,
    string Expiration,
    string CVV,
    int PaymentMethod,
    OrderStatus Status);
