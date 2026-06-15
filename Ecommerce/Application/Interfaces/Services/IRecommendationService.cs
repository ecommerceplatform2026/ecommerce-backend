using Application.Common.Response;
using Application.DTOs.Product;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IRecommendationService
    {
        Task<Result<bool>> TrackProductViewAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<Result<List<ProductResponse>>> GetPopularProductsAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ProductResponse>>> GetPersonalizedRecommendationsAsync(CancellationToken cancellationToken = default);
        Task<Result<List<ProductResponse>>> GetSimilarProductsAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<Result<int>> CleanupOldViewHistoriesAsync(CancellationToken cancellationToken = default);
    }
}
