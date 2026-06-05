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
        public long DiscountAmount { get; private set; }
        public OrderStatus Status { get; private set; }
        public int OrderCode { get; private set; }
        public PaymentMethod PaymentMethod { get; private set; }

        public User? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment? Payment { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
        public virtual Delivery? Delivery { get; set; }

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
            DiscountAmount = 0;
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

        public void ApplyDiscount(long discountAmount)
        {
            if (discountAmount < 0)
                throw new ArgumentException("Discount amount cannot be negative.", nameof(discountAmount));
            if (discountAmount > TotalAmount.Amount)
                throw new InvalidOperationException("Discount cannot exceed order total.");

            DiscountAmount = discountAmount;
        }

        public void ConfirmPayment()
        {
            if (Status != OrderStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm payment for an order in '{Status}' status.");

            Status = OrderStatus.Confirmed;
        }

        public void MarkAsProcessing()
        {
            if (Status != OrderStatus.Pending && Status != OrderStatus.Confirmed)
                throw new InvalidOperationException($"Cannot mark order in '{Status}' as Processing.");

            Status = OrderStatus.Processing;
        }

        public void MarkAsShipping()
        {
            if (Status != OrderStatus.Processing)
                throw new InvalidOperationException($"Cannot mark order in '{Status}' as Shipping.");

            Status = OrderStatus.Shipping;
        }

        public void MarkAsCancelled()
        {
            if (Status == OrderStatus.Cancelled)
                return;

            if (Status == OrderStatus.Delivered || Status == OrderStatus.Completed || Status == OrderStatus.Returned)
                throw new InvalidOperationException($"Cannot cancel an order in '{Status}' status.");

            Status = OrderStatus.Cancelled;
            AddDomainEvent(new Events.OrderCancelledDomainEvent(this));
        }

        public void MarkAsReturned()
        {
            if (Status == OrderStatus.Returned)
                return;

            if (Status != OrderStatus.Delivered)
                throw new InvalidOperationException($"Cannot return an order in '{Status}' status.");

            Status = OrderStatus.Returned;
            AddDomainEvent(new Events.OrderReturnedDomainEvent(this));
        }

        public bool CanBeReturned()
        {
            return Status == OrderStatus.Delivered
                && CreatedAt >= DateTime.UtcNow.AddDays(-7);
        }

        public void MarkAsDelivered()
        {
            if (Status == OrderStatus.Delivered)
            {
                return;
            }

            if (Status == OrderStatus.Cancelled || Status == OrderStatus.Returned)
            {
                throw new InvalidOperationException($"Cannot mark an order in '{Status}' status as delivered.");
            }

            Status = OrderStatus.Delivered;
            AddDomainEvent(new Events.OrderDeliveredDomainEvent(this));
        }

        public void MarkAsCompleted()
        {
            if (Status == OrderStatus.Completed)
            {
                return;
            }

            if (Status == OrderStatus.Cancelled || Status == OrderStatus.Returned)
            {
                throw new InvalidOperationException($"Cannot mark an order in '{Status}' status as completed.");
            }

            if (Status != OrderStatus.Delivered)
            {
                throw new InvalidOperationException($"Cannot mark an order in '{Status}' status as completed.");
            }

            Status = OrderStatus.Completed;
            AddDomainEvent(new Events.OrderCompletedDomainEvent(this));
        }
    }
}
