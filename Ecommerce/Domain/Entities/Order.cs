using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid UserId { get; set; }
        public long TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public int OrderCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }

        public User? User { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual Payment? Payment { get; set; }
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
