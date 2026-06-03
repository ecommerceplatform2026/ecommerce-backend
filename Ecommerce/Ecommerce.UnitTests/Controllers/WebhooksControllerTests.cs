using Application.Common.Response;
using Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ecommerce.UnitTests.Controllers
{
    public class WebhooksControllerTests
    {
        private readonly Mock<IShippingWebhookHandler> _handlerMock;
        private readonly WebhooksController _controller;

        public WebhooksControllerTests()
        {
            _handlerMock = new Mock<IShippingWebhookHandler>();
            _handlerMock.Setup(h => h.CarrierCode).Returns("GHN");

            _controller = new WebhooksController(new[] { _handlerMock.Object });
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithValidCarrierAndSuccessfulProcessing_Returns200WithSuccess()
        {
            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "delivered" });

            _handlerMock
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

            _handlerMock
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
            var controller = new WebhooksController(Enumerable.Empty<IShippingWebhookHandler>());

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

            _handlerMock
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

            var controller = new WebhooksController(new IShippingWebhookHandler[] { ghtkMock.Object, _handlerMock.Object });

            var payload = JsonSerializer.Serialize(new { order_code = "FFFNL9HH", status = "delivered" });
            _handlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = await controller.HandleDeliveryStatus("GHN", JsonDocument.Parse(payload).RootElement);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var success = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            success.Should().Be(true);
            _handlerMock.Verify(h => h.ProcessStatusUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
