using Application.Interfaces.Services;
using Application.Common.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Common.Extensions;
using Presentation.Common.Responses;
using Presentation.Controllers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class DeliveryControllerTests
    {
        private readonly Mock<IShippingService> _shippingServiceMock;
        private readonly DeliveryController _controller;

        public DeliveryControllerTests()
        {
            _shippingServiceMock = new Mock<IShippingService>();
            _controller = new DeliveryController(_shippingServiceMock.Object);
        }

        [Fact]
        public async Task CreateShipment_WithValidData_ReturnsOk()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<string>.Success("TRACK-001");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().Be("TRACK-001");
        }

        [Fact]
        public async Task CreateShipment_WhenServiceFails_ReturnsBadRequest()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<string>.Failure("Carrier API error");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Carrier API error");
        }

        [Fact]
        public async Task CreateShipment_UsesDefaultCarrierWhenNotSpecified()
        {
            var orderId = Guid.NewGuid();
            var serviceResult = Result<string>.Success("TRACK-001");

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
            var serviceResult = Result<string>.NotFound("Order not found.");

            _shippingServiceMock
                .Setup(s => s.CreateShipmentAsync(orderId, "GHN", It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            var result = await _controller.CreateShipment(orderId, "GHN", CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
