namespace Application.DTOs.Cart
{
    public sealed record MergeCartRequest(
        List<MergeCartItem> Items);

    public sealed record MergeCartItem(
        Guid ProductVariantId,
        int Quantity);
}
