using Application.DTOs.Checkout;
using Application.Interfaces.Services;
using Application.Common.Response;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class CheckoutControllerTests
    {
        private readonly Mock<ICheckoutService> _checkoutServiceMock;
        private readonly CheckoutController _controller;

        public CheckoutControllerTests()
        {
            _checkoutServiceMock = new Mock<ICheckoutService>();
            _controller = new CheckoutController(_checkoutServiceMock.Object);
        }

        [Fact]
        public async Task Checkout_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new CheckoutRequest(PaymentMethod.COD);

            var items = new List<CheckoutItemResponse>
            {
                new CheckoutItemResponse(Guid.NewGuid(), Guid.NewGuid(), 2, 100000, "snapshot")
            };
            var checkoutResponse = new CheckoutResponse(
                Guid.NewGuid(),
                12345,
                200000,
                0,
                200000,
                OrderStatus.Pending,
                PaymentMethod.COD,
                items,
                "http://checkout-url.com"
            );
            var serviceResult = Result<CheckoutResponse>.Success(checkoutResponse);

            _checkoutServiceMock
                .Setup(s => s.ProcessCheckoutAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Checkout(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CheckoutResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.OrderId.Should().Be(checkoutResponse.OrderId);
        }
    }
}
