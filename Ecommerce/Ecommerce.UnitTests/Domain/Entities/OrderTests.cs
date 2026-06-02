using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using FluentAssertions;
using System;
using System.Linq;
using System.Reflection;

namespace Ecommerce.UnitTests.EntityTests
{
    public class OrderTests
    {
        [Fact]
        public void MarkAsDelivered_WhenOrderCanBeDelivered_SetsStatusToDelivered()
        {
            var order = CreateOrder(OrderStatus.Confirmed);

            order.MarkAsDelivered();

            order.Status.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public void MarkAsDelivered_WhenOrderCanBeDelivered_RaisesOrderDeliveredDomainEvent()
        {
            var order = CreateOrder(OrderStatus.Shipping);

            order.MarkAsDelivered();

            order.DomainEvents.Should().ContainSingle(e => e is OrderDeliveredDomainEvent);
        }

        [Fact]
        public void MarkAsDelivered_WhenAlreadyDelivered_DoesNotRaiseDuplicateEvent()
        {
            var order = CreateOrder(OrderStatus.Confirmed);

            order.MarkAsDelivered();
            order.ClearDomainEvents();
            order.MarkAsDelivered();

            order.Status.Should().Be(OrderStatus.Delivered);
            order.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void MarkAsDelivered_WhenCancelled_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Cancelled);

            Action act = () => order.MarkAsDelivered();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Cancelled' status as delivered.");
        }

        [Fact]
        public void MarkAsDelivered_WhenReturned_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Returned);

            Action act = () => order.MarkAsDelivered();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Returned' status as delivered.");
        }

        private static Order CreateOrder(OrderStatus status)
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            SetStatus(order, status);
            order.ClearDomainEvents();
            return order;
        }

        private static void SetStatus(Order order, OrderStatus status)
        {
            typeof(Order)
                .GetProperty(nameof(Order.Status), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(order, status);
        }
    }
}
