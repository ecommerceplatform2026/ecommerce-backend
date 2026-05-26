namespace Application.DTOs.Product
{
    public sealed class ProductImageResponse
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
