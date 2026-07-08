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

        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly List<OrderItemResponse> Items = new()
        {
            new OrderItemResponse(Guid.NewGuid(), Guid.NewGuid(), 1, 150000, "snapshot")
        };

        [Fact]
        public async Task GetMyOrders_ReturnsOk_WithPagedList()
        {
            var request = new GetOrdersRequest { Page = 1, PageSize = 10, Status = OrderStatus.Pending };
            var orders = new List<OrderResponse> { new(OrderId, 10001, 150000, OrderStatus.Pending, PaymentMethod.COD, DateTime.UtcNow, Items) };
            var paged = new PagedResult<OrderResponse> { Items = orders, Page = 1, PageSize = 10, TotalCount = 1 };

            _orderServiceMock
                .Setup(s => s.GetMyOrdersAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PagedResult<OrderResponse>>.Success(paged));

            var result = await _controller.GetMyOrders(request, CancellationToken.None);

            var apiResponse = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<PagedResult<OrderResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Items.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetOrderById_WhenOrderHasTracking_ReturnsTrackingInfo()
        {
            var tracking = new TrackingInfo("TRACK-001", "GHN", DeliveryStatus.InTransit);
            var order = new OrderResponse(OrderId, 10001, 150000, OrderStatus.Pending, PaymentMethod.COD, DateTime.UtcNow, Items, tracking);

            _orderServiceMock
                .Setup(s => s.GetOrderByIdAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<OrderResponse>.Success(order));

            var result = await _controller.GetOrderById(OrderId, CancellationToken.None);

            var data = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<OrderResponse>>().Subject;
            data.Success.Should().BeTrue();
            data.Data!.Tracking.Should().NotBeNull();
            data.Data.Tracking!.TrackingCode.Should().Be("TRACK-001");
            data.Data.Tracking.CarrierCode.Should().Be("GHN");
            data.Data.Tracking.Status.Should().Be(DeliveryStatus.InTransit);
        }

        [Fact]
        public async Task GetOrderById_WhenOrderHasNoTracking_TrackingIsNull()
        {
            var order = new OrderResponse(OrderId, 10001, 150000, OrderStatus.Pending, PaymentMethod.COD, DateTime.UtcNow, Items);

            _orderServiceMock
                .Setup(s => s.GetOrderByIdAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<OrderResponse>.Success(order));

            var result = await _controller.GetOrderById(OrderId, CancellationToken.None);

            var data = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<OrderResponse>>().Subject;
            data.Data!.Tracking.Should().BeNull();
        }

        [Fact]
        public async Task GetOrderById_WhenOrderNotFound_Returns404()
        {
            _orderServiceMock
                .Setup(s => s.GetOrderByIdAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<OrderResponse>.NotFound("Order not found."));

            var result = await _controller.GetOrderById(OrderId, CancellationToken.None);

            var statusCodeResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        }

        [Fact]
        public async Task CompleteOrder_ReturnsOk_WithCompleteOrderResponse()
        {
            var response = new CompleteOrderResponse(OrderId, 10001, nameof(OrderStatus.Completed));

            _orderServiceMock
                .Setup(s => s.CompleteOrderAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<CompleteOrderResponse>.Success(response));

            var result = await _controller.CompleteOrder(OrderId, CancellationToken.None);

            var apiResponse = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<CompleteOrderResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Status.Should().Be(nameof(OrderStatus.Completed));
        }

        [Fact]
        public async Task CompleteOrder_WhenNotFound_Returns404()
        {
            _orderServiceMock
                .Setup(s => s.CompleteOrderAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<CompleteOrderResponse>.NotFound("Order not found."));

            var result = await _controller.CompleteOrder(OrderId, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ReturnOrder_ReturnsOk_WithReturnOrderResponse()
        {
            var response = new ReturnOrderResponse(OrderId, 10001, nameof(OrderStatus.Returned), DateTime.UtcNow.AddDays(7));

            _orderServiceMock
                .Setup(s => s.ReturnOrderAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ReturnOrderResponse>.Success(response));

            var result = await _controller.ReturnOrder(OrderId, CancellationToken.None);

            var apiResponse = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<ReturnOrderResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Status.Should().Be(nameof(OrderStatus.Returned));
        }

        [Fact]
        public async Task ReturnOrder_WhenNotFound_Returns404()
        {
            _orderServiceMock
                .Setup(s => s.ReturnOrderAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ReturnOrderResponse>.NotFound("Order not found."));

            var result = await _controller.ReturnOrder(OrderId, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMyOrders_WithTrackingList_IncludesTrackingInResponse()
        {
            var request = new GetOrdersRequest { Page = 1, PageSize = 10 };
            var tracking = new TrackingInfo("TRACK-001", "GHN", DeliveryStatus.InTransit);
            var orders = new List<OrderResponse> { new(OrderId, 10001, 150000, OrderStatus.Pending, PaymentMethod.COD, DateTime.UtcNow, Items, tracking) };
            var paged = new PagedResult<OrderResponse> { Items = orders, Page = 1, PageSize = 10, TotalCount = 1 };

            _orderServiceMock
                .Setup(s => s.GetMyOrdersAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PagedResult<OrderResponse>>.Success(paged));

            var result = await _controller.GetMyOrders(request, CancellationToken.None);

            var data = result.Should().BeOfType<OkObjectResult>().Subject
                .Value.Should().BeOfType<ApiResponse<PagedResult<OrderResponse>>>().Subject;
            data.Data!.Items[0].Tracking.Should().NotBeNull();
            data.Data.Items[0].Tracking!.TrackingCode.Should().Be("TRACK-001");
        }
    }
}
