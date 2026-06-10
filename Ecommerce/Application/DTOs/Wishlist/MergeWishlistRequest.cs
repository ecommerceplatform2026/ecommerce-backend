using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Wishlist
{
    public sealed record MergeWishlistRequest(
        [Required(ErrorMessage = "VariantIds list is required.")] List<Guid> VariantIds);
}
