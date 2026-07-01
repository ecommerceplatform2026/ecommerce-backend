using Application.Common.Response;
using Application.DTOs.Loyalty;
using Application.Interfaces.Services;
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
    public class LoyaltyControllerTests
    {
        private readonly Mock<ILoyaltyService> _loyaltyServiceMock;
        private readonly LoyaltyController _controller;

        public LoyaltyControllerTests()
        {
            _loyaltyServiceMock = new Mock<ILoyaltyService>();
            _controller = new LoyaltyController(_loyaltyServiceMock.Object);
        }

        [Fact]
        public async Task GetLoyaltyBalance_ReturnsOk_WithBalanceResponse()
        {
            // Arrange
            var balanceResponse = new GetLoyaltyBalanceResponse(
                Balance: 120,
                DiscountEquivalent: 1200000,
                LastUpdated: DateTime.UtcNow);

            _loyaltyServiceMock
                .Setup(s => s.GetLoyaltyBalanceAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<GetLoyaltyBalanceResponse>.Success(balanceResponse));

            // Act
            var result = await _controller.GetLoyaltyBalance(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GetLoyaltyBalanceResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Balance.Should().Be(120);
            apiResponse.Data.DiscountEquivalent.Should().Be(1200000);
        }

        [Fact]
        public async Task GetTransactionHistory_ReturnsOk_WithPagedTransactions()
        {
            // Arrange
            var request = new GetLoyaltyTransactionsRequest
            {
                Page = 1,
                PageSize = 10
            };

            var transaction = new GetLoyaltyTransactionResponse(
                Id: Guid.NewGuid(),
                Date: DateTime.UtcNow,
                Type: LoyaltyTransactionType.Earn,
                Points: 100,
                Status: LoyaltyTransactionStatus.Completed,
                OrderId: Guid.NewGuid().ToString(),
                Description: "Points earned from order");

            var pagedResult = new PagedResult<GetLoyaltyTransactionResponse>
            {
                Items = new List<GetLoyaltyTransactionResponse> { transaction },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };

            _loyaltyServiceMock
                .Setup(s => s.GetTransactionHistoryAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PagedResult<GetLoyaltyTransactionResponse>>.Success(pagedResult));

            // Act
            var result = await _controller.GetTransactionHistory(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<GetLoyaltyTransactionResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Items.Should().ContainSingle();
            apiResponse.Data.Page.Should().Be(1);
            apiResponse.Data.PageSize.Should().Be(10);
        }
    }
}
