using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid UserId { get; private set; }
        public Money TotalAmount { get; private set; } = null!;
        public OrderStatus Status { get; private set; }
        public int OrderCode { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }

        public User? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment? Payment { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

        private Order() { }

        private Order(Guid userId, int orderCode, PaymentMethod paymentMethod)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            if (orderCode <= 0)
                throw new ArgumentException("Order code must be greater than zero.", nameof(orderCode));

            UserId = userId;
            OrderCode = orderCode;
            PaymentMethod = paymentMethod;
            Status = OrderStatus.Pending;
            TotalAmount = Money.Zero("VND");
        }

        public static Order Create(Guid userId, int orderCode, PaymentMethod paymentMethod)
        {
            var order = new Order(userId, orderCode, paymentMethod);
            if (paymentMethod == PaymentMethod.COD)
            {
                order.AddDomainEvent(new Events.OrderCreatedDomainEvent(order));
            }
            return order;
        }

        public void AddItem(Guid productVariantId, int quantity, Money price, string productSnapshot)
        {
            var item = OrderItem.Create(Id, productVariantId, quantity, price, productSnapshot);
            OrderItems.Add(item);
            TotalAmount += price * quantity;
        }

        public void ConfirmPayment()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm payment for an order in '{Status}' status.");

            Status = OrderStatus.Confirmed;
        }

        public void Cancel()
        {
            if (Status == OrderStatus.Confirmed)
                throw new InvalidOperationException("Cannot cancel an order that has already been confirmed and paid.");

            Status = OrderStatus.Cancelled;
        }
    }
}
