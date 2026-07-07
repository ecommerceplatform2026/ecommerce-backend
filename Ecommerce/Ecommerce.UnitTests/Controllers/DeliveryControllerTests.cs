using Application.DTOs.Delivery;
using Application.DTOs.Delivery.GHN;
using Application.Interfaces.Services;
using Application.Common.Response;
using Domain.Enums;
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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
            var request = new CreateShipmentRequest(Guid.NewGuid());
            var serviceResult = Result<ShipmentResponse>.Success(SampleResponse);

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(It.Is<CreateShipmentRequest>(r => r.OrderId == request.OrderId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(request, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TrackingCode.Should().Be("TRACK-001");
        }

        [Fact]
        public async Task CreateShipment_WhenServiceFails_ReturnsBadRequest()
        {
            var request = new CreateShipmentRequest(Guid.NewGuid());
            var serviceResult = Result<ShipmentResponse>.Failure("Carrier API error");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(request, CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Carrier API error");
        }

        [Fact]
        public async Task CreateShipment_UsesDefaultCarrierWhenNotSpecified()
        {
            var request = new CreateShipmentRequest(Guid.NewGuid());
            var serviceResult = Result<ShipmentResponse>.Success(SampleResponse);

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            await _controller.CreateShipment(request, CancellationToken.None);

            _shippingServiceMock.Verify(
                s => s.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateShipment_WhenNotFound_ReturnsNotFound()
        {
            var request = new CreateShipmentRequest(Guid.NewGuid());
            var serviceResult = Result<ShipmentResponse>.NotFound("Order not found.");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(It.IsAny<CreateShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(request, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithValidPayload_ReturnsHandlerResponse()
        {
            var payload = new GhnWebhookPayload { OrderCode = "FFFNL9HH", Status = "delivered", Type = "switch_status" };
            var responseJson = JsonSerializer.Serialize(new GhnWebhookResponse
            {
                OrderCode = "FFFNL9HH",
                Status = "delivered",
                Type = "switch_status"
            });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(responseJson));

            var result = await _controller.HandleDeliveryStatus("GHN", payload);

            var contentResult = result.Should().BeOfType<ContentResult>().Subject;
            contentResult.Content.Should().Be(responseJson);
            contentResult.ContentType.Should().Be("application/json");
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithUnknownCarrier_Returns200WithErrorMessage()
        {
            var payload = new GhnWebhookPayload();
            var result = await _controller.HandleDeliveryStatus("UNKNOWN", payload);

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

            var payload = new GhnWebhookPayload();
            var result = await controller.HandleDeliveryStatus("GHN", payload);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            response.Should().NotBeNull();
            var success = response!.GetType().GetProperty("success")?.GetValue(response);
            success.Should().Be(false);
            var message = response!.GetType().GetProperty("message")?.GetValue(response);
            message.Should().Be("Unknown carrier 'GHN'.");
        }

        [Fact]
        public async Task HandleDeliveryStatus_AlwaysReturnsOkEvenOnHandlerResponseWithError()
        {
            var payload = new GhnWebhookPayload { OrderCode = "FFFNL9HH", Status = "delivered" };
            var errorJson = JsonSerializer.Serialize(new GhnWebhookResponse
            {
                OrderCode = "FFFNL9HH",
                Reason = "Some error",
                ReasonCode = "ERROR"
            });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(errorJson));

            var result = await _controller.HandleDeliveryStatus("GHN", payload);

            result.Should().BeOfType<ContentResult>();
        }

        [Fact]
        public async Task RetryShipment_WithValidDelivery_ReturnsOk()
        {
            var deliveryId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Success(SampleResponse);

            _shippingServiceMock
                .Setup(s => s.RetryShipmentAsync(deliveryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.RetryShipment(new RetryShipmentRequest(deliveryId), CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.TrackingCode.Should().Be("TRACK-001");
        }

        [Fact]
        public async Task RetryShipment_WhenDeliveryNotFound_ReturnsNotFound()
        {
            var deliveryId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.NotFound("Delivery not found.");

            _shippingServiceMock
                .Setup(s => s.RetryShipmentAsync(deliveryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.RetryShipment(new RetryShipmentRequest(deliveryId), CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RetryShipment_WhenDeliveryNotInException_ReturnsBadRequest()
        {
            var deliveryId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Failure("Cannot retry delivery in 'Delivered' status. Only Exception deliveries can be retried.");

            _shippingServiceMock
                .Setup(s => s.RetryShipmentAsync(deliveryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.RetryShipment(new RetryShipmentRequest(deliveryId), CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain(e => e.Contains("Exception deliveries can be retried"));
        }

        [Fact]
        public async Task RetryShipment_WhenServiceFails_ReturnsBadRequest()
        {
            var deliveryId = Guid.NewGuid();
            var serviceResult = Result<ShipmentResponse>.Failure("Shipping provider failed.");

            _shippingServiceMock
                .Setup(s => s.RetryShipmentAsync(deliveryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.RetryShipment(new RetryShipmentRequest(deliveryId), CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<ShipmentResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Shipping provider failed.");
        }

        [Fact]
        public async Task HandleDeliveryStatus_WithMultipleHandlers_ResolvesCorrectCarrier()
        {
            var ghtkMock = new Mock<IShippingWebhookHandler>();
            ghtkMock.Setup(h => h.CarrierCode).Returns("GHTK");
            ghtkMock.Setup(h => h.ProcessStatusUpdateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("{}"));

            var controller = new DeliveryController(
                _shippingServiceMock.Object,
                new IShippingWebhookHandler[] { ghtkMock.Object, _webhookHandlerMock.Object });

            var payload = new GhnWebhookPayload { OrderCode = "FFFNL9HH", Status = "delivered" };
            var responseJson = JsonSerializer.Serialize(new GhnWebhookResponse
            {
                OrderCode = "FFFNL9HH",
                Status = "delivered"
            });

            _webhookHandlerMock
                .Setup(h => h.ProcessStatusUpdateAsync(payload, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(responseJson));

            var result = await controller.HandleDeliveryStatus("GHN", payload);

            var contentResult = result.Should().BeOfType<ContentResult>().Subject;
            contentResult.Content.Should().Be(responseJson);
            _webhookHandlerMock.Verify(h => h.ProcessStatusUpdateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetDeliveries_WithNoFilters_ReturnsOk()
        {
            var request = new GetShipmentRequest();
            var paged = new PagedResult<ShipmentDetailResponse>
            {
                Items = new List<ShipmentDetailResponse> { SampleShipmentDetailResponse() },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };
            var serviceResult = Result<PagedResult<ShipmentDetailResponse>>.Success(paged);

            _shippingServiceMock
                .Setup(s => s.GetDeliveriesAsync(It.IsAny<GetShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetDeliveries(request, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ShipmentDetailResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetDeliveries_WithStatusFilter_ReturnsFilteredResults()
        {
            var request = new GetShipmentRequest { Status = DeliveryStatus.Created };
            var paged = new PagedResult<ShipmentDetailResponse>
            {
                Items = new List<ShipmentDetailResponse> { SampleShipmentDetailResponse() },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };
            var serviceResult = Result<PagedResult<ShipmentDetailResponse>>.Success(paged);

            _shippingServiceMock
                .Setup(s => s.GetDeliveriesAsync(It.IsAny<GetShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetDeliveries(request, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ShipmentDetailResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
        }

        [Fact]
        public async Task GetDeliveries_WhenServiceFails_ReturnsBadRequest()
        {
            var request = new GetShipmentRequest();
            var serviceResult = Result<PagedResult<ShipmentDetailResponse>>.Failure("Failed to retrieve deliveries.");

            _shippingServiceMock
                .Setup(s => s.GetDeliveriesAsync(It.IsAny<GetShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetDeliveries(request, CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<PagedResult<ShipmentDetailResponse>>>().Subject;
            apiResponse.Success.Should().BeFalse();
        }

        [Fact]
        public async Task GetDeliveries_PagingParams_ArePassedThrough()
        {
            var request = new GetShipmentRequest { Page = 2, PageSize = 5 };
            var paged = new PagedResult<ShipmentDetailResponse>
            {
                Items = new List<ShipmentDetailResponse>(),
                Page = 2,
                PageSize = 5,
                TotalCount = 0
            };
            var serviceResult = Result<PagedResult<ShipmentDetailResponse>>.Success(paged);

            _shippingServiceMock
                .Setup(s => s.GetDeliveriesAsync(It.IsAny<GetShipmentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.GetDeliveries(request, CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ShipmentDetailResponse>>>().Subject;
            apiResponse.Data!.Page.Should().Be(2);
            apiResponse.Data.PageSize.Should().Be(5);
        }

        private static ShipmentDetailResponse SampleShipmentDetailResponse()
        {
            return new ShipmentDetailResponse
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                CarrierCode = "GHN",
                TrackingCode = "TRACK-001",
                Status = DeliveryStatus.Pending,
                ToName = "Receiver",
                ToPhone = "0900000000",
                ToAddress = "123 Street",
                Province = "Province",
                District = "District",
                Ward = "Ward",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}