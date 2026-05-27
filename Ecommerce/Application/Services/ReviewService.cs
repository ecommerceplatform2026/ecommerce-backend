using Application.Common.Caching;
using Application.Common.Response;
using Application.DTOs.Review;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Security;
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
    public sealed class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUniqueConstraintChecker _uniqueConstraintChecker;
        private readonly ICacheService _cacheService;

        public ReviewService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IUniqueConstraintChecker uniqueConstraintChecker,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _uniqueConstraintChecker = uniqueConstraintChecker ?? throw new ArgumentNullException(nameof(uniqueConstraintChecker));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        private Result<Guid> GetCurrentUserId()
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Result<Guid>.Unauthorized("User is not authenticated.");
            }
            return Result<Guid>.Success(userId);
        }

        public async Task<Result<ReviewResponse>> CreateReviewAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return Result<ReviewResponse>.Failure("Request cannot be null.");
            }

            var userResult = GetCurrentUserId();
            if (!userResult.IsSuccess)
            {
                return Result<ReviewResponse>.Unauthorized(userResult.Errors.FirstOrDefault() ?? "Unauthorized");
            }

            var userId = userResult.Value;

            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(request.ProductId, cancellationToken);
            if (product == null)
            {
                return Result<ReviewResponse>.NotFound("Product not found.");
            }

            var order = await _unitOfWork.GetRepository<Order>().FindAsync(
                o => o.Id == request.OrderId && o.UserId == userId,
                true,
                cancellationToken,
                o => o.OrderItems,
                o => o.OrderItems.Select(oi => oi.ProductVariant!));

            if (order == null)
            {
                return Result<ReviewResponse>.NotFound("Order was not found or does not belong to the user.");
            }

            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            {
                return Result<ReviewResponse>.Failure("Cannot review products for pending or cancelled orders.");
            }

            var hasProduct = order.OrderItems.Any(oi => oi.ProductVariant != null && oi.ProductVariant.ProductId == request.ProductId);
            if (!hasProduct)
            {
                return Result<ReviewResponse>.Failure("This order does not contain the specified product.");
            }

            var existingReview = await _unitOfWork.GetRepository<Review>().FindAsync(
                r => r.UserId == userId && r.OrderId == request.OrderId && r.ProductId == request.ProductId,
                asNoTracking: true,
                cancellationToken: cancellationToken);

            if (existingReview != null)
            {
                return Result<ReviewResponse>.Conflict("You have already reviewed this product for this order.");
            }

            var review = Review.Create(
                userId,
                request.ProductId,
                request.OrderId,
                request.Rating,
                request.Title,
                request.Comment);

            try
            {
                await _unitOfWork.GetRepository<Review>().AddAsync(review, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex) when (_uniqueConstraintChecker.IsUniqueViolation(ex, "IX_Reviews_UserId_OrderId_ProductId"))
            {
                return Result<ReviewResponse>.Conflict("You have already reviewed this product for this order.");
            }

            await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(request.ProductId), cancellationToken);

            return Result<ReviewResponse>.Success(review.ToReviewResponse());
        }
    }
}
