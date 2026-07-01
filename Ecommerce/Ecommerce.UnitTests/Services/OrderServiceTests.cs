using Application.Common.Response;
using Application.DTOs.Order;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IGenericRepository<Order>> _orderRepositoryMock;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _orderRepositoryMock = new Mock<IGenericRepository<Order>>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Order>())
                .Returns(_orderRepositoryMock.Object);

            _service = new OrderService(_unitOfWorkMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task CancelOrderAsync_UnauthenticatedUser_ReturnsUnauthorized()
        {
            _currentUserServiceMock
                .Setup(s => s.GetUserIdOrNull())
                .Returns((string?)null);

            var result = await _service.CancelOrderAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("User is not authenticated.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_OrderNotFound_ReturnsNotFound()
        {
            SetupAuthenticatedUser();
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync((Order?)null);

            var result = await _service.CancelOrderAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CancelOrderAsync_ValidOrder_CancelsAndSaves()
        {
            SetupAuthenticatedUser();
            var order = CreateOrder(OrderStatus.Pending);
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CancelOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType<CancelOrderResponse>();
            result.Value!.OrderId.Should().Be(order.Id);
            order.Status.Should().Be(OrderStatus.Cancelled);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelOrderAsync_WhenDeliveredOrder_ThrowsInvalidOperationException()
        {
            SetupAuthenticatedUser();
            var order = CreateOrder(OrderStatus.Delivered);
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);

            Func<Task> act = () => _service.CancelOrderAsync(order.Id, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot cancel an order in 'Delivered' status.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteOrderAsync_UnauthenticatedUser_ReturnsUnauthorized()
        {
            _currentUserServiceMock
                .Setup(s => s.GetUserIdOrNull())
                .Returns((string?)null);

            var result = await _service.CompleteOrderAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("User is not authenticated.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteOrderAsync_OrderNotFound_ReturnsNotFound()
        {
            SetupAuthenticatedUser();
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync((Order?)null);

            var result = await _service.CompleteOrderAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CompleteOrderAsync_DeliveredOrder_CompletesAndSaves()
        {
            SetupAuthenticatedUser();
            var order = CreateOrder(OrderStatus.Delivered);
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);
            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CompleteOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType<CompleteOrderResponse>();
            result.Value!.OrderId.Should().Be(order.Id);
            order.Status.Should().Be(OrderStatus.Completed);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteOrderAsync_CompletedOrder_ReturnsSuccessIdempotent()
        {
            SetupAuthenticatedUser();
            var order = CreateOrder(OrderStatus.Completed);
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);

            var result = await _service.CompleteOrderAsync(order.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeOfType<CompleteOrderResponse>();
            order.Status.Should().Be(OrderStatus.Completed);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CompleteOrderAsync_ReturnedOrder_ThrowsInvalidOperationException()
        {
            SetupAuthenticatedUser();
            var order = CreateOrder(OrderStatus.Returned);
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);

            Func<Task> act = () => _service.CompleteOrderAsync(order.Id, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot mark an order in 'Returned' status as completed.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupAuthenticatedUser()
        {
            _currentUserServiceMock
                .Setup(s => s.GetUserIdOrNull())
                .Returns(Guid.NewGuid().ToString());
        }

        private static Order CreateOrder(OrderStatus status)
        {
            var order = Order.Create(Guid.NewGuid(), 100001, PaymentMethod.COD);
            order.ClearDomainEvents();
            typeof(Order)
                .GetProperty(nameof(Order.Status), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
                .SetValue(order, status);
            return order;
        }
    }
}
