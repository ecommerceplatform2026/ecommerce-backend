using Domain.Enums;

namespace Application.DTOs.Payment
{
    public sealed record PaymentResponse(
        Guid Id,
        Guid OrderId,
        int OrderCode,
        long Amount,
        PaymentStatus Status,
        string PaymentLinkId,
        string? CheckoutUrl);
}
