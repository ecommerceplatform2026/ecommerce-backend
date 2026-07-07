using Application.DTOs.Payment;
using Application.Interfaces.Services;
using Application.Common.Response;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class PaymentsControllerTests
    {
        private readonly Mock<IPaymentService> _paymentServiceMock;
        private readonly PaymentsController _controller;

        public PaymentsControllerTests()
        {
            _paymentServiceMock = new Mock<IPaymentService>();
            _controller = new PaymentsController(_paymentServiceMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_Amount=20000000&vnp_ResponseCode=00");
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task VnPayCallback_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var paymentResponse = new PaymentResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                12345,
                200000,
                PaymentStatus.Success,
                "payment_link_id",
                "http://checkout-url.com"
            );
            var serviceResult = Result<PaymentResponse>.Success(paymentResponse);

            _paymentServiceMock
                .Setup(s => s.ProcessVnPayCallbackAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.VnPayCallback(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = JsonSerializer.Serialize(okResult.Value);
            value.Should().Contain("RspCode");
        }

        [Fact]
        public async Task MomoCallback_ReturnsOk()
        {
            // Arrange
            var requestBody = new Dictionary<string, string> { { "resultCode", "0" } };

            _paymentServiceMock
                .Setup(s => s.ProcessMomoCallbackAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PaymentResponse>.Success(new PaymentResponse(Guid.NewGuid(), Guid.NewGuid(), 12345, 200000, PaymentStatus.Success, "link", "url")));

            // Act
            var result = await _controller.MomoCallback(requestBody, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ZaloPayCallback_ReturnsOk_WithReturnCode()
        {
            // Arrange
            var requestBody = new Dictionary<string, string>
            {
                { "app_id", "2553" },
                { "app_trans_id", "240101_12345" },
                { "amount", "50000" }
            };

            _paymentServiceMock
                .Setup(s => s.ProcessZaloPayCallbackAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PaymentResponse>.Success(new PaymentResponse(Guid.NewGuid(), Guid.NewGuid(), 12345, 200000, PaymentStatus.Success, "link", "url")));

            // Act
            var result = await _controller.ZaloPayCallback(requestBody, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var value = JsonSerializer.Serialize(okResult.Value);
            value.Should().Contain("return_code");
        }

        [Fact]
        public async Task GetPaymentStatus_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var paymentResponse = new PaymentResponse(Guid.NewGuid(), Guid.NewGuid(), 12345, 200000, PaymentStatus.Success, "link", "url");
            var serviceResult = Result<PaymentResponse>.Success(paymentResponse);

            _paymentServiceMock
                .Setup(s => s.GetPaymentStatusAsync(12345, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetPaymentStatus(12345, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PaymentResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
        }
    }
}
