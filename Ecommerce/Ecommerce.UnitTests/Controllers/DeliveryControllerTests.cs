using Application.DTOs.Delivery;
using Application.Interfaces.Services;
using Application.Common.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Common.Extensions;
using Presentation.Common.Responses;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class DeliveryControllerTests
    {
        private readonly Mock<IShippingService> _shippingServiceMock;
        private readonly Mock<IShippingWebhookHandler> _webhookHandlerMock;
        private readonly DeliveryController _controller;
        private static readonly ShipmentResponse SampleResponse = new("TRACK-001", "ORD-001", 35_000, "2026-06-05");

        public DeliveryControllerTests()
        {
            _shippingServiceMock = new Mock<IShippingService>();
            _webhookHandlerMock = new Mock<IShippingWebhookHandler>();
            _webhookHandlerMock.Setup(h => h.CarrierCode).Returns("GHN");

            _controller = new DeliveryController(
                _shippingServiceMock.Object,
                new[] { _webhookHandlerMock.Object });
        }

        [Fact]
        public async Task CreateShipment_WithValidData_ReturnsOk()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Success(SampleResponse);

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TrackingCode.Should().Be("TRACK-001");
        }

        [Fact]
        public async Task CreateShipment_WhenServiceFails_ReturnsBadRequest()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Failure("Carrier API error");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Carrier API error");
        }

        [Fact]
        public async Task CreateShipment_UsesDefaultCarrierWhenNotSpecified()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Success(SampleResponse);

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            await _controller.CreateShipment(orderId, null!, CancellationToken.None);

            _shippingServiceMock.Verify(
                s => s.CreateShipmentAsync(orderId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateShipment_WhenNotFound_ReturnsNotFound()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.NotFound("Order not found.");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithValidCarrierAndSuccessfulProcessing_Returns200WithSuccess()
        {
            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "delivered" });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await _controller.HandleDeliveryStatus("GHN", JsonDocument.Parse(payload).RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            var success = response!.GetType().GetProperty("success")?.GetValue(response);
            success.Should().Be(true);
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithValidCarrierAndFailedProcessing_Returns200WithErrorMessage()
        {
            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "unknown" });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Unknown GHN status"));

            var result = await _controller.HandleDeliveryStatus("GHN", JsonDocument.Parse(payload).RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            var success = response!.GetType().GetProperty("success")?.GetValue(response);
            success.Should().Be(false);
            var message = response!.GetType().GetProperty("message")?.GetValue(response);
            message.Should().Be("Unknown GHN status");
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithUnknownCarrier_Returns200WithErrorMessage()
        {
            var result = await _controller.HandleDeliveryStatus("UNKNOWN", JsonDocument.Parse("{}").RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            var success = response!.GetType().GetProperty("success")?.GetValue(response);
            success.Should().Be(false);
            var message = response!.GetType().GetProperty("message")?.GetValue(response);
            message.Should().Be("Unknown carrier 'UNKNOWN'.");
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithNoMatchingHandler_Returns200WithErrorMessage()
        {
            var controller = new DeliveryController(
                _shippingServiceMock.Object,
                Enumerable.Empty<IShippingWebhookHandler>());

            var result = await controller.HandleDeliveryStatus("GHN", JsonDocument.Parse("{}").RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            var success = response!.GetType().GetProperty("success")?.GetValue(response);
            success.Should().Be(false);
            var message = response!.GetType().GetProperty("message")?.GetValue(response);
            message.Should().Be("Unknown carrier 'GHN'.");
        }

        [Fact]
        public async Task HandleDeliveryStatus_AlwaysReturnsOkEvenOnFailure()
        {
            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "delivered" });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Some error"));

            var result = await _controller.HandleDeliveryStatus("GHN", JsonDocument.Parse(payload).RootElement);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithMultipleHandlers_ResolvesCorrectCarrier()
        {
            var ghtkMock = new Mock<IShippingWebhookHandler>();
            ghtkMock.Setup(h => h.CarrierCode).Returns("GHTK");
            ghtkMock.Setup(h => h.ProcessStatusUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Should not be called"));

            var controller = new DeliveryController(
                _shippingServiceMock.Object,
                new IShippingWebhookHandler[] { ghtkMock.Object, _webhookHandlerMock.Object });

            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "delivered" });
            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await controller.HandleDeliveryStatus("GHN", JsonDocument.Parse(payload).RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var success = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            success.Should().Be(true);
            _webhookHandlerMock.Verify(h => h.ProcessStatusUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
