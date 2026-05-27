using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Cart
{
    public sealed record UpdateCartItemRequest(
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")] int Quantity);
}
