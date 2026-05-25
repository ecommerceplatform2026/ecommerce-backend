using Domain.Enums;

namespace Application.DTOs.Checkout
{
    public sealed record CheckoutRequest(
        PaymentMethod PaymentMethod);
}
