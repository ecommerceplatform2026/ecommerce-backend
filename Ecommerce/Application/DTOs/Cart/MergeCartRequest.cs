using Application.Common.Validations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Cart
{
    public sealed record MergeCartRequest(
        [Required(ErrorMessage = "Items list is required.")] List<MergeCartItem> Items);

    public sealed record MergeCartItem(
        [NotEmptyGuid(ErrorMessage = "ProductVariantId is required.")] Guid ProductVariantId,
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")] int Quantity);
}
