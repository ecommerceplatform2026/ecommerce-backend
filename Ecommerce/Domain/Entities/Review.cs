using Domain.Common;
using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Review : BaseEntity
    {
        public Guid UserId { get; private set; }
        public Guid ProductId { get; private set; }
        public Guid OrderId { get; private set; }
        public int Rating { get; private set; }
        public string? Title { get; private set; }
        public string? Comment { get; private set; }
        public ReviewStatus Status { get; private set; }

        public User? User { get; set; }
        public Product? Product { get; set; }
        public Order? Order { get; set; }

        private Review() { }

        private Review(Guid userId, Guid productId, Guid orderId, int rating, string? title, string? comment)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            if (productId == Guid.Empty)
                throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
            if (orderId == Guid.Empty)
                throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
            if (rating < 1 || rating > 5)
                throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

            UserId = userId;
            ProductId = productId;
            OrderId = orderId;
            Rating = rating;
            Title = title?.Trim();
            Comment = comment?.Trim();
            Status = ReviewStatus.Approved;
        }

        public static Review Create(Guid userId, Guid productId, Guid orderId, int rating, string? title, string? comment)
        {
            return new Review(userId, productId, orderId, rating, title, comment);
        }

        public void Approve() => Status = ReviewStatus.Approved;
        public void Reject() => Status = ReviewStatus.Rejected;
    }
}
