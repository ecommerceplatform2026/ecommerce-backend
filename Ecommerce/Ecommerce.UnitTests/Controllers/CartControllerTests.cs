using Application.DTOs.Cart;
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
    public class CartControllerTests
    {
        private readonly Mock<ICartService> _cartServiceMock;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            _cartServiceMock = new Mock<ICartService>();
            _controller = new CartController(_cartServiceMock.Object);
        }

        private CartItemResponse CreateDummyCartItemResponse(Guid id, Guid variantId)
        {
            return new CartItemResponse(
                id,
                variantId,
                Guid.NewGuid(),
                "Product Name",
                "http://image.png",
                "SKU001",
                "Black",
                "M",
                150000,
                2,
                100,
                false,
                false
            );
        }

        [Fact]
        public async Task GetCart_ReturnsOk_WithList()
        {
            // Arrange
            var items = new List<CartItemResponse>
            {
                CreateDummyCartItemResponse(Guid.NewGuid(), Guid.NewGuid()),
                CreateDummyCartItemResponse(Guid.NewGuid(), Guid.NewGuid())
            };
            var serviceResult = Result<List<CartItemResponse>>.Success(items);

            _cartServiceMock
                .Setup(s => s.GetCartAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetCart(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CartItemResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task AddToCart_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var variantId = Guid.NewGuid();
            var request = new AddToCartRequest(variantId, 2);
            var response = CreateDummyCartItemResponse(Guid.NewGuid(), variantId);
            var serviceResult = Result<CartItemResponse>.Success(response);

            _cartServiceMock
                .Setup(s => s.AddToCartAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.AddToCart(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CartItemResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Quantity.Should().Be(2);
        }

        [Fact]
        public async Task UpdateCartItem_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var variantId = Guid.NewGuid();
            var request = new UpdateCartItemRequest(5);
            var response = CreateDummyCartItemResponse(Guid.NewGuid(), variantId);
            // update quantity in dummy response
            response = response with { Quantity = 5 };
            var serviceResult = Result<CartItemResponse>.Success(response);

            _cartServiceMock
                .Setup(s => s.UpdateCartItemAsync(variantId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateCartItem(variantId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CartItemResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Quantity.Should().Be(5);
        }

        [Fact]
        public async Task RemoveCartItem_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var variantId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _cartServiceMock
                .Setup(s => s.RemoveCartItemAsync(variantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.RemoveCartItem(variantId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task MergeCart_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var variantId = Guid.NewGuid();
            var request = new MergeCartRequest(
                new List<MergeCartItem>
                {
                    new MergeCartItem(variantId, 2)
                }
            );
            var items = new List<CartItemResponse>
            {
                CreateDummyCartItemResponse(Guid.NewGuid(), variantId)
            };
            var serviceResult = Result<List<CartItemResponse>>.Success(items);

            _cartServiceMock
                .Setup(s => s.MergeCartAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.MergeCart(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CartItemResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(1);
        }
    }
}
