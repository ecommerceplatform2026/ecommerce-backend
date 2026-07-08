using Domain.Common;
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

        [Fact]
        public void MarkAsCompleted_WhenOrderCanBeCompleted_SetsStatusToCompleted()
        {
            var order = CreateOrder(OrderStatus.Delivered);

            order.MarkAsCompleted();

            order.Status.Should().Be(OrderStatus.Completed);
        }

        [Fact]
        public void MarkAsCompleted_WhenOrderCanBeCompleted_RaisesOrderCompletedDomainEvent()
        {
            var order = CreateOrder(OrderStatus.Delivered);

            order.MarkAsCompleted();

            order.DomainEvents.Should().ContainSingle(e => e is OrderCompletedDomainEvent);
        }

        [Fact]
        public void MarkAsCompleted_WhenAlreadyCompleted_DoesNotRaiseDuplicateEvent()
        {
            var order = CreateOrder(OrderStatus.Delivered);

            order.MarkAsCompleted();
            order.ClearDomainEvents();
            order.MarkAsCompleted();

            order.Status.Should().Be(OrderStatus.Completed);
            order.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void MarkAsCompleted_WhenCancelled_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Cancelled);

            Action act = () => order.MarkAsCompleted();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Cancelled' status as completed.");
        }

        [Fact]
        public void MarkAsCompleted_WhenReturned_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Returned);

            Action act = () => order.MarkAsCompleted();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Returned' status as completed.");
        }

        [Fact]
        public void MarkAsCompleted_WhenNotDelivered_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Confirmed);

            Action act = () => order.MarkAsCompleted();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Confirmed' status as completed.");
        }

        [Fact]
        public void ApplyDiscount_ValidAmount_SetsDiscount()
        {
            var order = CreateOrder(OrderStatus.Pending);
            order.AddItem(Guid.NewGuid(), 2, new Money(50000), "snapshot");

            order.ApplyDiscount(30000);

            order.DiscountAmount.Should().Be(30000);
        }

        [Fact]
        public void ApplyDiscount_NegativeAmount_Throws()
        {
            var order = CreateOrder(OrderStatus.Pending);

            Action act = () => order.ApplyDiscount(-1000);

            act.Should().Throw<ArgumentException>()
                .WithMessage("Discount amount cannot be negative.*");
        }

        [Fact]
        public void ApplyDiscount_ExceedsTotal_Throws()
        {
            var order = CreateOrder(OrderStatus.Pending);
            order.AddItem(Guid.NewGuid(), 1, new Money(50000), "snapshot");

            Action act = () => order.ApplyDiscount(60000);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Discount cannot exceed order total.");
        }

        [Fact]
        public void MarkAsCancelled_WhenPending_SetsCancelledAndRaisesEvent()
        {
            var order = CreateOrder(OrderStatus.Pending);

            order.MarkAsCancelled();

            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
        }

        [Fact]
        public void MarkAsCancelled_WhenProcessing_SetsCancelledAndRaisesEvent()
        {
            var order = CreateOrder(OrderStatus.Processing);

            order.MarkAsCancelled();

            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
        }

        [Fact]
        public void MarkAsCancelled_WhenShipping_SetsCancelledAndRaisesEvent()
        {
            var order = CreateOrder(OrderStatus.Shipping);

            order.MarkAsCancelled();

            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
        }

        [Fact]
        public void MarkAsCancelled_WhenConfirmed_SetsCancelledAndRaisesEvent()
        {
            var order = CreateOrder(OrderStatus.Confirmed);

            order.MarkAsCancelled();

            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledDomainEvent);
        }

        [Fact]
        public void MarkAsCancelled_WhenAlreadyCancelled_NoOp()
        {
            var order = CreateOrder(OrderStatus.Cancelled);

            order.MarkAsCancelled();

            order.Status.Should().Be(OrderStatus.Cancelled);
            order.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void MarkAsCancelled_WhenDelivered_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Delivered);

            Action act = () => order.MarkAsCancelled();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot cancel an order in 'Delivered' status.");
        }

        [Fact]
        public void MarkAsCancelled_WhenCompleted_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Completed);

            Action act = () => order.MarkAsCancelled();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot cancel an order in 'Completed' status.");
        }

        [Fact]
        public void MarkAsCancelled_WhenReturned_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Returned);

            Action act = () => order.MarkAsCancelled();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot cancel an order in 'Returned' status.");
        }

        [Fact]
        public void MarkAsReturned_WhenDelivered_SetsReturnedAndRaisesEvent()
        {
            var order = CreateOrder(OrderStatus.Delivered);

            order.MarkAsReturned();

            order.Status.Should().Be(OrderStatus.Returned);
            order.DomainEvents.Should().ContainSingle(e => e is OrderReturnedDomainEvent);
        }

        [Fact]
        public void MarkAsReturned_WhenAlreadyReturned_NoOp()
        {
            var order = CreateOrder(OrderStatus.Returned);

            order.MarkAsReturned();

            order.Status.Should().Be(OrderStatus.Returned);
            order.DomainEvents.Should().BeEmpty();
        }

        [Fact]
        public void MarkAsReturned_WhenNotDelivered_ThrowsInvalidOperationException()
        {
            var order = CreateOrder(OrderStatus.Pending);

            Action act = () => order.MarkAsReturned();

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot return an order in 'Pending' status.");
        }

        [Fact]
        public void CanBeReturned_WhenDeliveredAndWithinWindow_ReturnsTrue()
        {
            var order = CreateOrder(OrderStatus.Delivered);
            typeof(BaseEntity)
                .GetProperty(nameof(BaseEntity.CreatedAt), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(order, DateTime.UtcNow);

            var result = order.CanBeReturned();

            result.Should().BeTrue();
        }

        [Fact]
        public void CanBeReturned_WhenDeliveredAndPastWindow_ReturnsFalse()
        {
            var order = CreateOrder(OrderStatus.Delivered);
            typeof(BaseEntity)
                .GetProperty(nameof(BaseEntity.CreatedAt), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(order, DateTime.UtcNow.AddDays(-8));

            var result = order.CanBeReturned();

            result.Should().BeFalse();
        }

        [Fact]
        public void CanBeReturned_WhenNotDelivered_ReturnsFalse()
        {
            var order = CreateOrder(OrderStatus.Pending);

            var result = order.CanBeReturned();

            result.Should().BeFalse();
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
