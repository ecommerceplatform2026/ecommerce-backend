using Application.Common.Response;
using Application.DTOs.Product;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Mappings;
using Domain.Entities;
using Domain.Enums;
using Domain.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class RecommendationService : IRecommendationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;

        public RecommendationService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<bool>> TrackProductViewAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr))
                return Result<bool>.Success(false);

            var role = _currentUserService.GetUserRoleOrNull();
            if (role != "User")
                return Result<bool>.Success(false);

            var userId = Guid.Parse(userIdStr);

            // Verify product exists and is active
            var productExists = await _unitOfWork.GetRepository<Product>().TotalAsync(
                p => p.Id == productId && !p.IsDeleted && p.Status == ProductStatus.Active
            ) > 0;

            if (!productExists)
                return Result<bool>.Success(false);

            var repo = _unitOfWork.GetRepository<RecentlyViewedProduct>();
            var existing = await repo.FindAsync(
                rv => rv.UserId == userId && rv.ProductId == productId,
                asNoTracking: false,
                cancellationToken: cancellationToken
            );

            if (existing != null)
            {
                existing.SetCreated(userIdStr);
                repo.Update(existing);
            }
            else
            {
                var newView = RecentlyViewedProduct.Create(userId, productId);
                newView.SetCreated(userIdStr);
                await repo.AddAsync(newView, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Trim view history to max 20 items per user
            var allViews = await repo.GetAllTrackedAsync(
                rv => rv.UserId == userId,
                cancellationToken
            );

            if (allViews.Count > 20)
            {
                var toDelete = allViews
                    .OrderByDescending(rv => rv.CreatedAt)
                    .Skip(20)
                    .ToList();

                foreach (var oldView in toDelete)
                {
                    repo.Remove(oldView);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Invalidate personalized cache for this user
            await _cacheService.RemoveAsync($"recommendations:foryou:{userId}", cancellationToken);

            return Result<bool>.Success(true);
        }

        public async Task<Result<List<ProductResponse>>> GetPopularProductsAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = "recommendations:popular";
            var cached = await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var salesQuantities = await GetRecentSalesQuantitiesAsync(cancellationToken);
                    
                    var activeProducts = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                        p => p.Status == ProductStatus.Active &&
                             !p.IsDeleted &&
                             p.Category.Status == CategoryStatus.Active &&
                             !p.Category.IsDeleted,
                        cancellationToken,
                        p => p.Category,
                        p => p.ProductVariants,
                        p => p.ProductImages,
                        p => p.Reviews
                    );

                    var inStockProducts = activeProducts
                        .Where(p => p.ProductVariants.Any(v => !v.IsDeleted && v.Stock > 0))
                        .ToList();

                    var rankedProducts = inStockProducts
                        .OrderByDescending(p => salesQuantities.TryGetValue(p.Id, out var qty) ? qty : 0)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(20)
                        .Select(p => p.ToProductResponse())
                        .ToList();

                    return rankedProducts;
                },
                TimeSpan.FromHours(1),
                cancellationToken
            );

            return Result<List<ProductResponse>>.Success(cached ?? new List<ProductResponse>());
        }

        public async Task<Result<List<ProductResponse>>> GetPersonalizedRecommendationsAsync(CancellationToken cancellationToken = default)
        {
            var userIdStr = _currentUserService.GetUserIdOrNull();
            if (string.IsNullOrEmpty(userIdStr))
            {
                // Cold start fallback for guest / anonymous
                var coldStartResult = await GetColdStartRecommendationsAsync(cancellationToken);
                return Result<List<ProductResponse>>.Success(coldStartResult);
            }

            var userId = Guid.Parse(userIdStr);
            var cacheKey = $"recommendations:foryou:{userId}";

            var cached = await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var viewHistory = await _unitOfWork.GetRepository<RecentlyViewedProduct>().GetAllAsync(
                        rv => rv.UserId == userId && !rv.IsDeleted,
                        cancellationToken,
                        rv => rv.Product!
                    );

                    var purchaseHistory = await _unitOfWork.GetRepository<OrderItem>().GetAllAsync(
                        oi => oi.Order!.UserId == userId &&
                              oi.Order.Status != OrderStatus.Cancelled &&
                              oi.Order.Status != OrderStatus.Returned &&
                              oi.Order.Status != OrderStatus.Pending,
                        cancellationToken,
                        oi => oi.Order!,
                        oi => oi.ProductVariant!.Product!
                    );

                    var seenProductIds = new HashSet<Guid>();
                    foreach (var view in viewHistory)
                    {
                        seenProductIds.Add(view.ProductId);
                    }
                    foreach (var item in purchaseHistory)
                    {
                        if (item.ProductVariant?.Product != null)
                        {
                            seenProductIds.Add(item.ProductVariant.Product.Id);
                        }
                    }

                    var categoryScores = new Dictionary<Guid, int>();
                    foreach (var view in viewHistory)
                    {
                        if (view.Product != null)
                        {
                            var catId = view.Product.CategoryId;
                            categoryScores[catId] = categoryScores.TryGetValue(catId, out var s) ? s + 1 : 1;
                        }
                    }
                    foreach (var item in purchaseHistory)
                    {
                        if (item.ProductVariant?.Product != null)
                        {
                            var catId = item.ProductVariant.Product.CategoryId;
                            categoryScores[catId] = categoryScores.TryGetValue(catId, out var s) ? s + 3 : 3;
                        }
                    }

                    var activeProducts = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                        p => p.Status == ProductStatus.Active &&
                             !p.IsDeleted &&
                             p.Category.Status == CategoryStatus.Active &&
                             !p.Category.IsDeleted,
                        cancellationToken,
                        p => p.Category,
                        p => p.ProductVariants,
                        p => p.ProductImages,
                        p => p.Reviews
                    );

                    var inStockProducts = activeProducts
                        .Where(p => p.ProductVariants.Any(v => !v.IsDeleted && v.Stock > 0))
                        .ToList();

                    var candidates = inStockProducts
                        .Where(p => !seenProductIds.Contains(p.Id))
                        .ToList();

                    if (categoryScores.Count == 0)
                    {
                        // No history cold start fallback
                        return candidates
                            .OrderByDescending(p => p.CreatedAt)
                            .Take(20)
                            .Select(p => p.ToProductResponse())
                            .ToList();
                    }

                    var salesQuantities = await GetRecentSalesQuantitiesAsync(cancellationToken);

                    var recommended = candidates
                        .Where(p => categoryScores.ContainsKey(p.CategoryId))
                        .OrderByDescending(p => categoryScores.TryGetValue(p.CategoryId, out var score) ? score : 0)
                        .ThenByDescending(p => salesQuantities.TryGetValue(p.Id, out var qty) ? qty : 0)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(20)
                        .ToList();

                    if (recommended.Count < 20)
                    {
                        var remainingCandidates = candidates
                            .Where(p => !recommended.Any(r => r.Id == p.Id))
                            .OrderByDescending(p => p.CreatedAt)
                            .ToList();

                        var fillCount = 20 - recommended.Count;
                        recommended.AddRange(remainingCandidates.Take(fillCount));
                    }

                    return recommended.Select(p => p.ToProductResponse()).ToList();
                },
                TimeSpan.FromMinutes(30),
                cancellationToken
            );

            return Result<List<ProductResponse>>.Success(cached ?? new List<ProductResponse>());
        }

        public async Task<Result<List<ProductResponse>>> GetSimilarProductsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var baseProduct = await _unitOfWork.GetRepository<Product>().FindAsync(
                p => p.Id == productId && !p.IsDeleted,
                true,
                cancellationToken,
                p => p.ProductVariants
            );

            if (baseProduct == null)
                return Result<List<ProductResponse>>.NotFound("Product not found.");

            var cacheKey = $"recommendations:similar:{productId}";
            var cached = await _cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var candidates = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                        p => p.CategoryId == baseProduct.CategoryId &&
                             p.Id != baseProduct.Id &&
                             p.Status == ProductStatus.Active &&
                             !p.IsDeleted &&
                             p.Category.Status == CategoryStatus.Active &&
                             !p.Category.IsDeleted,
                        cancellationToken,
                        p => p.Category,
                        p => p.ProductVariants,
                        p => p.ProductImages,
                        p => p.Reviews
                    );

                    var inStockCandidates = candidates
                        .Where(p => p.ProductVariants.Any(v => !v.IsDeleted && v.Stock > 0))
                        .ToList();

                    var salesQuantities = await GetRecentSalesQuantitiesAsync(cancellationToken);

                    var basePrice = baseProduct.BasePrice.Amount;

                    // 1. Narrow range: ±30% range
                    var narrowMatches = inStockCandidates
                        .Where(p => p.BasePrice.Amount >= basePrice * 0.7 &&
                                    p.BasePrice.Amount <= basePrice * 1.3)
                        .OrderByDescending(p => salesQuantities.TryGetValue(p.Id, out var qty) ? qty : 0)
                        .ThenByDescending(p => p.CreatedAt)
                        .ToList();

                    if (narrowMatches.Count >= 4)
                    {
                        return narrowMatches
                            .Take(10)
                            .Select(p => p.ToProductResponse())
                            .ToList();
                    }

                    // 2. Wide range: ±50% range
                    var wideMatches = inStockCandidates
                        .Where(p => p.BasePrice.Amount >= basePrice * 0.5 &&
                                    p.BasePrice.Amount <= basePrice * 1.5)
                        .OrderByDescending(p => salesQuantities.TryGetValue(p.Id, out var qty) ? qty : 0)
                        .ThenByDescending(p => p.CreatedAt)
                        .Take(10)
                        .Select(p => p.ToProductResponse())
                        .ToList();

                    return wideMatches;
                },
                TimeSpan.FromHours(3),
                cancellationToken
            );

            return Result<List<ProductResponse>>.Success(cached ?? new List<ProductResponse>());
        }

        public async Task<Result<int>> CleanupOldViewHistoriesAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = TimeHelper.GetTime().AddDays(-30);
            var repo = _unitOfWork.GetRepository<RecentlyViewedProduct>();
            
            var oldRecords = await repo.GetAllTrackedAsync(
                rv => rv.CreatedAt < cutoff,
                cancellationToken
            );

            foreach (var record in oldRecords)
            {
                repo.Remove(record);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(oldRecords.Count);
        }

        private async Task<Dictionary<Guid, int>> GetRecentSalesQuantitiesAsync(CancellationToken cancellationToken)
        {
            var thirtyDaysAgo = TimeHelper.GetTime().AddDays(-30);
            var orderItems = await _unitOfWork.GetRepository<OrderItem>().GetAllAsync(
                oi => oi.Order!.CreatedAt >= thirtyDaysAgo &&
                      oi.Order.Status != OrderStatus.Cancelled &&
                      oi.Order.Status != OrderStatus.Returned &&
                      oi.Order.Status != OrderStatus.Pending,
                cancellationToken,
                oi => oi.Order!,
                oi => oi.ProductVariant!
            );

            return orderItems
                .GroupBy(oi => oi.ProductVariant!.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(oi => oi.Quantity));
        }

        private async Task<List<ProductResponse>> GetColdStartRecommendationsAsync(CancellationToken cancellationToken)
        {
            var activeProducts = await _unitOfWork.GetRepository<Product>().GetAllAsync(
                p => p.Status == ProductStatus.Active &&
                     !p.IsDeleted &&
                     p.Category.Status == CategoryStatus.Active &&
                     !p.Category.IsDeleted,
                cancellationToken,
                p => p.Category,
                p => p.ProductVariants,
                p => p.ProductImages,
                p => p.Reviews
            );

            var inStockProducts = activeProducts
                .Where(p => p.ProductVariants.Any(v => !v.IsDeleted && v.Stock > 0))
                .ToList();

            return inStockProducts
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .Select(p => p.ToProductResponse())
                .ToList();
        }
    }
}
