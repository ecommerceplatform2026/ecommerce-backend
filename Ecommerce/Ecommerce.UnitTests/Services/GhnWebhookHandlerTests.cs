using Application.Common.Response;
using Application.DTOs.Delivery.GHN;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Application.Services;
using Moq;
using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.UnitTests.Services
{
    public class GhnWebhookHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Delivery>> _deliveryRepoMock;
        private readonly Mock<IGenericRepository<Order>> _orderRepoMock;
        private readonly IShippingWebhookHandler _handler;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GhnWebhookHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _deliveryRepoMock = new Mock<IGenericRepository<Delivery>>();
            _orderRepoMock = new Mock<IGenericRepository<Order>>();

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Delivery>())
                .Returns(_deliveryRepoMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<Order>())
                .Returns(_orderRepoMock.Object);

            _handler = new GhnWebhookService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenPicking_MarksDeliveryPickedUp()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.Created, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "picking"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.PickedUp);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenPicked_MarksDeliveryInTransit()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.PickedUp, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "picked"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenStoring_MarksDeliveryInTransit()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.PickedUp, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "storing"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenTransporting_MarksDeliveryInTransit()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.PickedUp, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "transporting"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenSorting_MarksDeliveryInTransit()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.PickedUp, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "sorting"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenDelivering_MarksDeliveryOutForDeliveryAndOrderShipping()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "delivering"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.OutForDelivery);
            order.Status.Should().Be(OrderStatus.Shipping);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenDelivered_MarksDeliveryDeliveredAndOrderDelivered()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.OutForDelivery, OrderStatus.Shipping);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "delivered"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Delivered);
            order.Status.Should().Be(OrderStatus.Delivered);
            order.DomainEvents.Should().Contain(e => e.GetType().Name == "OrderDeliveredDomainEvent");
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenDeliveryFail_MarksDeliveryFailed()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "delivery_fail", reason: "Receiver not home"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Failed);
            delivery.Note.Should().Be("Receiver not home");
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenReturn_MarksDeliveryReturned()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Delivered);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "return"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Returned);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenReturned_MarksDeliveryReturned()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Delivered);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "returned"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Returned);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenCancel_MarksDeliveryCancelled()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "cancel"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Cancelled);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenDamage_MarksDeliveryException()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "damage", reason: "Package damaged"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Exception);
            delivery.Note.Should().Be("Package damaged");
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenLost_MarksDeliveryException()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.InTransit, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "lost", reason: "Package lost"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Exception);
            delivery.Note.Should().Be("Package lost");
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_ReadyToPick_IsNoOp()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.Created, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "ready_to_pick"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Created);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WithInvalidPayloadType_ReturnsErrorInResponse()
        {
            var result = await _handler.ProcessStatusUpdateAsync(
                "not a GhnWebhookPayload", CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var response = JsonSerializer.Deserialize<GhnWebhookResponse>(result.Value!, JsonOptions);
            response.Should().NotBeNull();
            response!.Reason.Should().Contain("Invalid webhook payload");
            response.ReasonCode.Should().Be("ERROR");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WithMissingOrderCode_ReturnsErrorInResponse()
        {
            var payload = new GhnWebhookPayload { Status = "delivered", Type = "switch_status" };

            var result = await _handler.ProcessStatusUpdateAsync(payload, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var response = JsonSerializer.Deserialize<GhnWebhookResponse>(result.Value!, JsonOptions);
            response.Should().NotBeNull();
            response!.Reason.Should().Contain("Missing OrderCode");
            response.ReasonCode.Should().Be("ERROR");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenDeliveryNotFound_ReturnsErrorInResponse()
        {
            _deliveryRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Delivery, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Delivery, object>>[]>()))
                .ReturnsAsync((Delivery?)null);

            SetupOrder(null);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("UNKNOWN", "delivered"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var response = JsonSerializer.Deserialize<GhnWebhookResponse>(result.Value!, JsonOptions);
            response.Should().NotBeNull();
            response!.Reason.Should().Contain("not found");
            response.OrderCode.Should().Be("UNKNOWN");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WhenOrderNotFound_ReturnsErrorInResponse()
        {
            var (delivery, _) = CreateShipment(DeliveryStatus.Created, OrderStatus.Processing);
            SetupDelivery(delivery);
            _orderRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync((Order?)null);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "picking"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var response = JsonSerializer.Deserialize<GhnWebhookResponse>(result.Value!, JsonOptions);
            response.Should().NotBeNull();
            response!.Reason.Should().Contain("Order for delivery");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WithAnyTransition_UpdatesStatus()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.Created, OrderStatus.Confirmed);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "picking"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.PickedUp);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_DuplicateDelivered_IsIdempotent()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.Delivered, OrderStatus.Delivered);
            order.ClearDomainEvents();
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "delivered"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(DeliveryStatus.Delivered);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessStatusUpdateAsync_WithUnknownGhnStatus_ReturnsErrorInResponse()
        {
            var (delivery, order) = CreateShipment(DeliveryStatus.Created, OrderStatus.Processing);
            SetupDelivery(delivery);
            SetupOrder(order);

            var result = await _handler.ProcessStatusUpdateAsync(
                BuildPayload("FFFNL9HH", "unknown_status"), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            var response = JsonSerializer.Deserialize<GhnWebhookResponse>(result.Value!, JsonOptions);
            response.Should().NotBeNull();
            response!.Reason.Should().Contain("Unknown GHN status");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupDelivery(Delivery delivery)
        {
            _deliveryRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Delivery, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Delivery, object>>[]>()))
                .ReturnsAsync(delivery);
        }

        private void SetupOrder(Order? order)
        {
            _orderRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);
        }

        private static (Delivery delivery, Order order) CreateShipment(DeliveryStatus deliveryStatus, OrderStatus orderStatus)
        {
            var order = Order.Create(Guid.NewGuid(), 100001, Domain.Enums.PaymentMethod.COD);
            SetOrderStatus(order, orderStatus);
            order.ClearDomainEvents();

            var delivery = Delivery.Create(
                order.Id, "GHN",
                "Receiver", "0900000000", "123 Street",
                "Province", "District", "Ward",
                500, 10, 10, 10,
                100_000, 150_000,
                "Order #100001");

            delivery.MarkShipmentCreated("TRACK-001", "FFFNL9HH", 35_000);
            SetDeliveryStatus(delivery, deliveryStatus);

            return (delivery, order);
        }

        private static GhnWebhookPayload BuildPayload(string orderCode, string status, string? reason = null, string type = "switch_status")
        {
            return new GhnWebhookPayload
            {
                OrderCode = orderCode,
                Status = status,
                Type = type,
                Time = "2026-06-03T14:00:00Z",
                Reason = reason ?? ""
            };
        }

        private static void SetOrderStatus(Order order, OrderStatus status)
        {
            typeof(Order)
                .GetProperty(nameof(Order.Status), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
                .SetValue(order, status);
        }

        private static void SetDeliveryStatus(Delivery delivery, DeliveryStatus status)
        {
            typeof(Delivery)
                .GetProperty(nameof(Delivery.Status), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
                .SetValue(delivery, status);
        }
    }
}