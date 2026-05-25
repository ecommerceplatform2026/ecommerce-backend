namespace Application.DTOs.Payment
{
    public sealed record ProcessPaymentRequest(
        string PaymentLinkId,
        bool IsSuccess);
}
