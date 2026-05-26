using Application.DTOs.Order;
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
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _controller = new OrdersController(_orderServiceMock.Object);
        }

        [Fact]
        public async Task GetMyOrders_ReturnsOk_WithPagedList()
        {
            // Arrange
            var request = new GetOrdersRequest
            {
                Page = 1,
                PageSize = 10,
                Status = OrderStatus.Pending
            };

            var items = new List<OrderItemResponse>
            {
                new OrderItemResponse(Guid.NewGuid(), Guid.NewGuid(), 1, 150000, "snapshot")
            };
            var orderResponses = new List<OrderResponse>
            {
                new OrderResponse(Guid.NewGuid(), 10001, 150000, OrderStatus.Pending, PaymentMethod.COD, DateTime.UtcNow, items)
            };

            var pagedResult = new PagedResult<OrderResponse>
            {
                Items = orderResponses,
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };
            var serviceResult = Result<PagedResult<OrderResponse>>.Success(pagedResult);

            _orderServiceMock
                .Setup(s => s.GetMyOrdersAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetMyOrders(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<OrderResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }
    }
}
