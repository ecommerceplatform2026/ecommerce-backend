namespace Application.DTOs.Dashboard
{
    public sealed record PaymentMethodSummaryResponse(
        string Method,
        long Count,
        long Revenue);
}
