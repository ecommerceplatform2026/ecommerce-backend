using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Cart
{
    public sealed record AddToCartRequest(
        [NotEmptyGuid(ErrorMessage = "ProductVariantId is required.")] Guid ProductVariantId,
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")] int Quantity);
}
