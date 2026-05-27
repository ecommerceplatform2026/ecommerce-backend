using Application.Common.Response;
using Application.DTOs.Dashboard;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            try
            {
                // 1. Base query for orders
                var orderQuery = _unitOfWork.GetRepository<Order>().GetQueryable()
                    .Where(o => !o.IsDeleted);

                if (request.StartDate.HasValue)
                {
                    orderQuery = orderQuery.Where(o => o.CreatedAt >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    orderQuery = orderQuery.Where(o => o.CreatedAt <= request.EndDate.Value);
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
                // Group by Product ID and Name, summing the quantities sold in valid orders
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
                    .Include(pv => pv.Product)
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
            catch (Exception ex)
            {
                return Result<DashboardSummaryResponse>.Failure($"An error occurred while loading dashboard statistics: {ex.Message}");
            }
        }
    }
}
