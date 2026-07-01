using Application.Common.Response;
using Application.DTOs.Order;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;

        public OrderService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<Result<PagedResult<OrderResponse>>> GetMyOrdersAsync(GetOrdersRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<PagedResult<OrderResponse>>.Failure("Request cannot be null.");
            }

            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<PagedResult<OrderResponse>>.Unauthorized("User is not authenticated.");
            }

            System.Linq.Expressions.Expression<Func<Order, bool>> filter = o => o.UserId == userId && !o.IsDeleted;
            if (request.Status.HasValue)
            {
                filter = o => o.UserId == userId && !o.IsDeleted && o.Status == request.Status.Value;
            }

            var (orders, totalCount) = await _unitOfWork.GetRepository<Order>()
                .GetPagedAsync(
                    page: request.Page,
                    pageSize: request.PageSize,
                    filter: filter,
                    orderBy: o => o.CreatedAt,
                    isDescending: true,
                    cancellationToken: cancellationToken,
                    o => o.OrderItems,
                    o => o.Delivery!,
                    o => o.LoyaltyTransactions!);

            var result = new PagedResult<OrderResponse>
            {
                Items = orders.Select(o => o.ToOrderResponse()).ToList(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Result<PagedResult<OrderResponse>>.Success(result);
        }

        public async Task<Result<OrderResponse>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Result<OrderResponse>.Unauthorized("User is not authenticated.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == id && o.UserId == userId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken: cancellationToken,
                    o => o.OrderItems,
                    o => o.Delivery!,
                    o => o.LoyaltyTransactions!);

            if (order == null)
                return Result<OrderResponse>.NotFound("Order not found.");

            return Result<OrderResponse>.Success(order.ToOrderResponse());
        }

        public async Task<Result<CompleteOrderResponse>> CompleteOrderAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Result<CompleteOrderResponse>.Unauthorized("User is not authenticated.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == id && o.UserId == userId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (order == null)
                return Result<CompleteOrderResponse>.NotFound("Order not found.");

            order.MarkAsCompleted();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CompleteOrderResponse>.Success(new CompleteOrderResponse(
                order.Id,
                order.OrderCode,
                order.Status.ToString()));
        }

        public async Task<Result<ReturnOrderResponse>> ReturnOrderAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Result<ReturnOrderResponse>.Unauthorized("User is not authenticated.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == id && o.UserId == userId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (order == null)
                return Result<ReturnOrderResponse>.NotFound("Order not found.");

            if (!order.CanBeReturned())
                return Result<ReturnOrderResponse>.Failure("Order cannot be returned. It must be in Delivered status and within 7-day return window.");

            order.MarkAsReturned();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ReturnOrderResponse>.Success(new ReturnOrderResponse(
                order.Id,
                order.OrderCode,
                order.Status.ToString(),
                order.CreatedAt.AddDays(7)));
        }

        public async Task<Result<CancelOrderResponse>> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Result<CancelOrderResponse>.Unauthorized("User is not authenticated.");

            var order = await _unitOfWork.GetRepository<Order>()
                .FindAsync(
                    o => o.Id == id && o.UserId == userId && !o.IsDeleted,
                    asNoTracking: false,
                    cancellationToken);

            if (order == null)
                return Result<CancelOrderResponse>.NotFound("Order not found.");

            order.MarkAsCancelled();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<CancelOrderResponse>.Success(new CancelOrderResponse(
                order.Id,
                order.OrderCode,
                order.Status.ToString(),
                null,
                null));
        }
    }
}
