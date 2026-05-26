using Application.DTOs.Dashboard;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly EcommerceContext _context;

        public DashboardRepository(EcommerceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Base query for orders
            var orderQuery = _context.Set<Order>()
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
            var totalRevenue = await validOrdersQuery.SumAsync(o => o.TotalAmount.Amount, cancellationToken);

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
            var orderItems = await validOrdersQuery
                .SelectMany(o => o.OrderItems)
                .Where(oi => !oi.IsDeleted && oi.ProductVariant != null && oi.ProductVariant.Product != null)
                .Select(oi => new
                {
                    oi.ProductVariant!.ProductId,
                    ProductName = oi.ProductVariant.Product.Name,
                    oi.Quantity,
                    oi.Price
                })
                .ToListAsync(cancellationToken);

            var topSellingProducts = orderItems
                .GroupBy(oi => new { oi.ProductId, oi.ProductName })
                .Select(g => new TopSellingProductResponse(
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Sum(oi => (long)oi.Quantity),
                    g.Sum(oi => oi.Price.Amount * oi.Quantity)))
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(5)
                .ToList();

            // 6. Low stock variants monitoring (current real-time snapshot)
            var lowStockVariants = await _context.Set<ProductVariant>()
                .Include(pv => pv.Product)
                .Where(pv => !pv.IsDeleted && pv.Product != null && pv.Stock <= pv.LowStockThreshold)
                .Select(pv => new LowStockVariantResponse(
                    pv.Id,
                    pv.SKU.Value,
                    pv.Product.Name,
                    pv.Color,
                    pv.Size,
                    pv.Stock,
                    pv.LowStockThreshold))
                .ToListAsync(cancellationToken);

            return new DashboardSummaryResponse(
                totalOrders,
                totalRevenue,
                topSellingProducts,
                lowStockVariants,
                orderStatusSummary);
        }
    }
}
