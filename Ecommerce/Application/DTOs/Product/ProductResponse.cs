using Domain.Enums;

namespace Application.DTOs.Product
{
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Material { get; set; }
        public long BasePrice { get; set; }
        public ProductStatus Status { get; set; }
        public string? CategoryName { get; set; }
    }
}
