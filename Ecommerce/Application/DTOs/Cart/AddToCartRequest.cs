namespace Application.DTOs.Cart
{
    public sealed record AddToCartRequest(
        Guid ProductVariantId,
        int Quantity);
}
