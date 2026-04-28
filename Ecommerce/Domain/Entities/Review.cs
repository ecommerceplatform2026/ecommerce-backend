using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public Guid OrderId { get; set; }
        public int Rating { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public ReviewStatus Status { get; set; }

        public User? User { get; set; }
        public Product? Product { get; set; }
        public Order? Order { get; set; }
    }
}
