using Application.Common.Response;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Product
{
    public sealed class ProductListingRequest : PagingRequest
    {
        public string? Search { get; set; }
        
        public Guid? CategoryId { get; set; }
        
        public string? Category { get; set; }
        
        [Range(0, long.MaxValue, ErrorMessage = "Min price must be non-negative.")]
        public long? MinPrice { get; set; }
        
        [Range(0, long.MaxValue, ErrorMessage = "Max price must be non-negative.")]
        public long? MaxPrice { get; set; }
        
        public string? Size { get; set; }
        
        public string? Color { get; set; }
        
        public string? Material { get; set; }
        
        public string? SortBy { get; set; } = "createdAt";
        
        [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be either 'asc' or 'desc'.")]
        public string? SortDirection { get; set; } = "desc";
    }
}
