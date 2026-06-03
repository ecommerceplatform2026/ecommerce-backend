using Application.Common.Response;
using Application.Configurations;
using Application.DTOs.Delivery;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.UnitTests.Services
{
    public class ShippingServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGenericRepository<Order>> _orderRepositoryMock;
        private readonly Mock<IGenericRepository<ProductVariant>> _variantRepositoryMock;
        private readonly Mock<IGenericRepository<Delivery>> _deliveryRepositoryMock;
        private readonly Mock<IShippingProvider> _providerMock;
        private readonly ShippingSettings _settings;
        private readonly ShippingService _service;

        public ShippingServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _orderRepositoryMock = new Mock<IGenericRepository<Order>>();
            _variantRepositoryMock = new Mock<IGenericRepository<ProductVariant>>();
            _deliveryRepositoryMock = new Mock<IGenericRepository<Delivery>>();
            _providerMock = new Mock<IShippingProvider>();
            _settings = new ShippingSettings();

            _providerMock.Setup(p => p.CarrierCode).Returns("GHN");

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Order>())
                .Returns(_orderRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<ProductVariant>())
                .Returns(_variantRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(u => u.GetRepository<Delivery>())
                .Returns(_deliveryRepositoryMock.Object);

            _service = new ShippingService(
                _unitOfWorkMock.Object,
                new[] { _providerMock.Object },
                Options.Create(_settings));
        }

        [Fact]
        public async Task CreateShipmentAsync_WithEmptyOrderId_ReturnsFailure()
        {
            var result = await _service.CreateShipmentAsync(Guid.Empty, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order ID cannot be empty.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WithUnknownCarrier_ReturnsFailure()
        {
            var result = await _service.CreateShipmentAsync(Guid.NewGuid(), "UNKNOWN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("No shipping provider found for carrier 'UNKNOWN'.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WhenOrderNotFound_ReturnsNotFound()
        {
            SetupOrder(null);

            var result = await _service.CreateShipmentAsync(Guid.NewGuid(), "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Order not found.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WithInvalidOrderStatus_ReturnsFailure()
        {
            var order = BuildOrder(OrderStatus.Shipping);
            SetupOrder(order);

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Cannot create shipment for order in 'Shipping' status.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WhenDuplicateShipment_ReturnsFailure()
        {
            var order = BuildOrder();
            order.Delivery = Delivery.Create(
                order.Id, "GHN", "R", "P", "A", "Prov", "Dist", "Ward",
                500, 10, 10, 10, 0, 0, null);
            SetupOrder(order);

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Shipment already exists for this order.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WithNoAddress_ReturnsFailure()
        {
            var order = BuildOrder(withAddress: false);
            SetupOrder(order);

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("User has no shipping address.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_WhenVariantOutOfStock_ReturnsFailure()
        {
            var variantId = Guid.NewGuid();
            var order = BuildOrder(variantId: variantId);
            var variant = ProductVariant.Create(
                variantId, new Domain.Common.Sku("OUT-OF-STOCK"),
                null, null, 0, new Domain.Common.Money(100_000));
            SetupOrder(order);
            SetupVariant(variant);

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain($"Product for variant {variantId} is out of stock.");
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_UsesDefaultCarrierWhenOmitted()
        {
            var order = BuildOrder();
            SetupOrder(order);
            SetupVariantInStock();

            _providerMock
                .Setup(p => p.CreateShipmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CreateGhnShipmentRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ShipmentResponse>.Success(
                    new ShipmentResponse("TRACK-1", "ORD-1", 35_000, "2026-06-05")));

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CreateShipmentAsync(order.Id, "", CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TrackingCode.Should().Be("TRACK-1");
            _providerMock.Verify(p => p.CreateShipmentAsync(
                It.IsAny<Guid>(),
                It.IsAny<CreateGhnShipmentRequest>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateShipmentAsync_WhenProviderFails_ReturnsFailureAndOrderUnchanged()
        {
            var order = BuildOrder();
            SetupOrder(order);
            SetupVariantInStock();

            _providerMock
                .Setup(p => p.CreateShipmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CreateGhnShipmentRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ShipmentResponse>.Failure("Carrier API error"));

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Carrier API error");
            _deliveryRepositoryMock.Verify(
                r => r.AddAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateShipmentAsync_OnSuccess_CreatesDeliveryAndTransitionsOrder()
        {
            var order = BuildOrder();
            Delivery? addedDelivery = null;

            SetupOrder(order);
            SetupVariantInStock();
            _deliveryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
                .Callback<Delivery, CancellationToken>((d, _) => addedDelivery = d)
                .Returns(Task.CompletedTask);

            _providerMock
                .Setup(p => p.CreateShipmentAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CreateGhnShipmentRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ShipmentResponse>.Success(
                    new ShipmentResponse("TRACK-001", "GHN-ORDER-123", 35_000, "2026-06-05")));

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.TrackingCode.Should().Be("TRACK-001");

            addedDelivery.Should().NotBeNull();
            addedDelivery!.OrderId.Should().Be(order.Id);
            addedDelivery.CarrierCode.Should().Be("GHN");
            addedDelivery.TrackingCode.Should().Be("TRACK-001");
            addedDelivery.CarrierOrderCode.Should().Be("GHN-ORDER-123");
            addedDelivery.ShippingFee.Should().Be(35_000);
            addedDelivery.Status.Should().Be(DeliveryStatus.Created);

            order.Status.Should().Be(OrderStatus.Shipping);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateShipmentAsync_WithCODPayment_SetsCodAmount()
        {
            var order = BuildOrder(payment: PaymentMethod.COD, amount: 200_000);
            Delivery? addedDelivery = null;

            SetupOrder(order);
            SetupVariantInStock();
            _deliveryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
                .Callback<Delivery, CancellationToken>((d, _) => addedDelivery = d)
                .Returns(Task.CompletedTask);

            _providerMock
                .Setup(p => p.CreateShipmentAsync(
                    It.IsAny<Guid>(),
                    It.Is<CreateGhnShipmentRequest>(r => r.CodAmount == 200_000),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ShipmentResponse>.Success(
                    new ShipmentResponse("TRACK-COD", "ORD-COD", 35_000, null)));

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            addedDelivery!.CodAmount.Should().Be(200_000);
        }

        [Fact]
        public async Task CreateShipmentAsync_WithOnlinePayment_ZeroCodAmount()
        {
            var order = BuildOrder(payment: PaymentMethod.VNPay, amount: 200_000);
            Delivery? addedDelivery = null;

            SetupOrder(order);
            SetupVariantInStock();
            _deliveryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Delivery>(), It.IsAny<CancellationToken>()))
                .Callback<Delivery, CancellationToken>((d, _) => addedDelivery = d)
                .Returns(Task.CompletedTask);

            _providerMock
                .Setup(p => p.CreateShipmentAsync(
                    It.IsAny<Guid>(),
                    It.Is<CreateGhnShipmentRequest>(r => r.CodAmount == 0),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ShipmentResponse>.Success(
                    new ShipmentResponse("TRACK-ONLINE", "ORD-ONLINE", 35_000, null)));

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            await _service.CreateShipmentAsync(order.Id, "GHN", CancellationToken.None);

            addedDelivery!.CodAmount.Should().Be(0);
        }

        private void SetupOrder(Order? order)
        {
            _orderRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Order, bool>>>(),
                    false,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<Order, object>>[]>()))
                .ReturnsAsync(order);
        }

        private void SetupVariant(ProductVariant? variant)
        {
            _variantRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ProductVariant, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<ProductVariant, object>>[]>()))
                .ReturnsAsync(variant);
        }

        private void SetupVariantInStock()
        {
            var variant = ProductVariant.Create(
                Guid.NewGuid(), new Domain.Common.Sku("IN-STOCK"),
                null, null, 100, new Domain.Common.Money(100_000));
            _variantRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ProductVariant, bool>>>(),
                    true,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<ProductVariant, object>>[]>()))
                .ReturnsAsync(variant);
        }

        private static Order BuildOrder(
            OrderStatus status = OrderStatus.Pending,
            PaymentMethod payment = PaymentMethod.COD,
            long amount = 150_000,
            bool withAddress = true,
            Guid? variantId = null)
        {
            var user = User.Create("Test User", "test@test.com", "hash");
            if (withAddress)
            {
                user.AddAddress("Receiver", "0900000000", "123 Street",
                    "Ward 1", "District 1", "Province 1", isDefault: true);
            }

            var order = Order.Create(user.Id, 100001, payment);
            order.ClearDomainEvents();
            order.User = user;
            SetOrderStatus(order, status);

            var snapshot = """{"ProductName":"Test Product","SKU":"TST-001","CategoryName":"Electronics","Weight":500}""";
            order.AddItem(variantId ?? Guid.NewGuid(), 1, new Domain.Common.Money(amount), snapshot);

            return order;
        }

        private static void SetOrderStatus(Order order, OrderStatus status)
        {
            typeof(Order)
                .GetProperty(nameof(Order.Status), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(order, status);
        }
    }
}
