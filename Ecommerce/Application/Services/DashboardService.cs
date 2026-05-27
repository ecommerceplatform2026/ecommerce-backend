using Application.Common.Response;
using Application.DTOs.Dashboard;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<DashboardSummaryResponse>> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Base query for orders
            var orderQuery = _unitOfWork.GetRepository<Order>().GetQueryable()
                .AsNoTracking()
                .Where(o => !o.IsDeleted);

            if (request.StartDate.HasValue)
            {
                orderQuery = orderQuery.Where(o => o.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value;
                if (endDate.TimeOfDay == TimeSpan.Zero)
                {
                    var exclusiveEndDate = endDate.Date.AddDays(1);
                    orderQuery = orderQuery.Where(o => o.CreatedAt < exclusiveEndDate);
                }
                else
                {
                    orderQuery = orderQuery.Where(o => o.CreatedAt <= endDate);
                }
            }

            // 2. Count total orders in the date range
            var totalOrders = await orderQuery.CountAsync(cancellationToken);

            // 3. Calculate total revenue for valid orders in the date range (not Pending, not Cancelled)
            var validOrdersQuery = orderQuery.Where(o => o.Status != OrderStatus.Pending && o.Status != OrderStatus.Cancelled);
            var totalRevenue = await validOrdersQuery.SumAsync(o => o.TotalAmount, cancellationToken);

            // 4. Order status summary in the date range
            var statusGroups = await orderQuery
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var orderStatusSummary = Enum.GetValues<OrderStatus>()
                .ToDictionary(s => s.ToString(), _ => 0);

            foreach (var group in statusGroups)
            {
                orderStatusSummary[group.Status.ToString()] = group.Count;
            }

            // 5. Top selling products sold within the specified date range
            var topSellingProducts = await validOrdersQuery
                .SelectMany(o => o.OrderItems)
                .Where(oi => !oi.IsDeleted && oi.ProductVariant != null && oi.ProductVariant.Product != null)
                .GroupBy(oi => new { oi.ProductVariant!.ProductId, oi.ProductVariant.Product.Name })
                .Select(g => new TopSellingProductResponse(
                    g.Key.ProductId,
                    g.Key.Name,
                    g.Sum(oi => (long)oi.Quantity),
                    g.Sum(oi => oi.Price * oi.Quantity)))
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(5)
                .ToListAsync(cancellationToken);

            // 6. Low stock variants monitoring (current real-time snapshot)
            var lowStockVariants = await _unitOfWork.GetRepository<ProductVariant>().GetQueryable()
                .AsNoTracking()
                .Where(pv => !pv.IsDeleted && pv.Product != null && pv.Stock <= pv.LowStockThreshold)
                .Select(pv => new LowStockVariantResponse(
                    pv.Id,
                    pv.SKU,
                    pv.Product.Name,
                    pv.Color,
                    pv.Size,
                    pv.Stock,
                    pv.LowStockThreshold))
                .ToListAsync(cancellationToken);

            var response = new DashboardSummaryResponse(
                totalOrders,
                totalRevenue,
                topSellingProducts,
                lowStockVariants,
                orderStatusSummary);

            return Result<DashboardSummaryResponse>.Success(response);
        }
    }
}
