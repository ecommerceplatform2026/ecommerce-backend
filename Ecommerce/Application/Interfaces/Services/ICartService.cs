using Application.Common.Response;
using Application.DTOs.Cart;

namespace Application.Interfaces.Services
{
    public interface ICartService
    {
        Task<Result<List<CartItemResponse>>> GetCartAsync(CancellationToken cancellationToken = default);
        Task<Result<CartItemResponse>> AddToCartAsync(AddToCartRequest request, CancellationToken cancellationToken = default);
        Task<Result<CartItemResponse>> UpdateCartItemAsync(Guid variantId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> RemoveCartItemAsync(Guid variantId, CancellationToken cancellationToken = default);
        Task<Result<List<CartItemResponse>>> MergeCartAsync(MergeCartRequest request, CancellationToken cancellationToken = default);
    }
}
