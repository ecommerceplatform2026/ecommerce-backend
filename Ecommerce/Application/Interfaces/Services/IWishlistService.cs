using Application.Common.Response;
using Application.DTOs.Wishlist;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IWishlistService
    {
        Task<Result<List<WishlistItemResponse>>> GetWishlistAsync(List<Guid>? guestVariantIds = null, CancellationToken cancellationToken = default);
        Task<Result<WishlistItemResponse>> AddToWishlistAsync(AddToWishlistRequest request, CancellationToken cancellationToken = default);
        Task<Result<bool>> RemoveFromWishlistAsync(Guid variantId, CancellationToken cancellationToken = default);
        Task<Result<List<WishlistItemResponse>>> MergeWishlistAsync(MergeWishlistRequest request, CancellationToken cancellationToken = default);
    }
}
