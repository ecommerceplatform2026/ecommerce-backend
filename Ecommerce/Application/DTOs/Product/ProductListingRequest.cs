using Application.Common.Response;

namespace Application.DTOs.Product
{
    public sealed class ProductListingRequest : PagingRequest
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Category { get; set; }
        public long? MinPrice { get; set; }
        public long? MaxPrice { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? Material { get; set; }
        public string? SortBy { get; set; } = "createdAt";
        public string? SortDirection { get; set; } = "desc";
    }
}
