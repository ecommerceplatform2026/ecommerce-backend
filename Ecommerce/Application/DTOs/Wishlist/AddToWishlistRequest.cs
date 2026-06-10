using Application.Common.Validations;
using System;

namespace Application.DTOs.Wishlist
{
    public sealed record AddToWishlistRequest(
        [NotEmptyGuid(ErrorMessage = "ProductVariantId is required.")] Guid ProductVariantId);
}
