namespace Application.DTOs.Dashboard
{
    public sealed record DashboardRequest(
        DateTime? StartDate,
        DateTime? EndDate);

    public sealed record DashboardSummaryResponse(
        long TotalOrders,
        long TotalRevenue,
        List<TopSellingProductResponse> TopSellingProducts,
        List<LowStockVariantResponse> LowStockVariants,
        Dictionary<string, int> OrderStatusSummary);

    public sealed record TopSellingProductResponse(
        Guid ProductId,
        string ProductName,
        long TotalQuantitySold,
        long TotalRevenueGenerated);

    public sealed record LowStockVariantResponse(
        Guid VariantId,
        string SKU,
        string ProductName,
        string? Color,
        string? Size,
        long CurrentStock,
        long LowStockThreshold);
}
