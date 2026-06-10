using Application.DTOs.Dashboard;
using Application.Interfaces.Services;
using Application.Common.Response;
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
    public class AdminDashboardControllerTests
    {
        private readonly Mock<IDashboardService> _dashboardServiceMock;
        private readonly AdminDashboardController _controller;

        public AdminDashboardControllerTests()
        {
            _dashboardServiceMock = new Mock<IDashboardService>();
            _controller = new AdminDashboardController(_dashboardServiceMock.Object);
        }

        [Fact]
        public async Task GetSummary_ReturnsBadRequest_WhenStartDateAfterEndDate()
        {
            // Arrange
            var request = new DashboardRequest(
                StartDate: DateTime.UtcNow.AddDays(1),
                EndDate: DateTime.UtcNow
            );

            // Act
            var result = await _controller.GetSummary(request, CancellationToken.None);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("StartDate cannot be after EndDate.");
        }

        [Fact]
        public async Task GetSummary_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new DashboardRequest(
                StartDate: DateTime.UtcNow.AddDays(-7),
                EndDate: DateTime.UtcNow
            );

            var summaryResponse = new DashboardSummaryResponse(
                TotalOrders: 10,
                TotalRevenue: 1500000,
                TopSellingProducts: new List<TopSellingProductResponse>(),
                LowStockVariants: new List<LowStockVariantResponse>(),
                OrderStatusSummary: new Dictionary<string, int>()
            );
            var serviceResult = Result<DashboardSummaryResponse>.Success(summaryResponse);

            _dashboardServiceMock
                .Setup(s => s.GetDashboardSummaryAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetSummary(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DashboardSummaryResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.TotalOrders.Should().Be(10);
        }

        [Fact]
        public async Task GetRevenueTrend_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new DashboardRequest(
                StartDate: DateTime.UtcNow.AddDays(-7),
                EndDate: DateTime.UtcNow
            );

            var trendResponse = new List<RevenueTrendResponse>
            {
                new RevenueTrendResponse(DateTime.UtcNow.ToString("yyyy-MM-dd"), 1500000, 10)
            };
            var serviceResult = Result<List<RevenueTrendResponse>>.Success(trendResponse);

            _dashboardServiceMock
                .Setup(s => s.GetRevenueTrendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetRevenueTrend(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<RevenueTrendResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetPaymentMethodSummary_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new DashboardRequest(
                StartDate: DateTime.UtcNow.AddDays(-7),
                EndDate: DateTime.UtcNow
            );

            var summaryResponse = new List<PaymentMethodSummaryResponse>
            {
                new PaymentMethodSummaryResponse("COD", 5, 500000)
            };
            var serviceResult = Result<List<PaymentMethodSummaryResponse>>.Success(summaryResponse);

            _dashboardServiceMock
                .Setup(s => s.GetPaymentMethodSummaryAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetPaymentMethodSummary(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<PaymentMethodSummaryResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(1);
        }
    }
}
