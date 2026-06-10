namespace Application.DTOs.Dashboard
{
    public sealed record RevenueTrendResponse(
        string Date,
        long Revenue,
        long Orders);
}
