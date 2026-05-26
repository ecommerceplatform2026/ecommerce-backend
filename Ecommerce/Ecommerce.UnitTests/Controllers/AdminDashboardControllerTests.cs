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
            badRequestResult.Value.Should().Be("StartDate cannot be after EndDate.");
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
    }
}
