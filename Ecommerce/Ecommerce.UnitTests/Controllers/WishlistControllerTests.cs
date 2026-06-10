using Application.Common.Response;
using Application.DTOs.Wishlist;
using Application.Interfaces.Services;
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
    public class WishlistControllerTests
    {
        private readonly Mock<IWishlistService> _wishlistServiceMock;
        private readonly WishlistController _controller;

        public WishlistControllerTests()
        {
            _wishlistServiceMock = new Mock<IWishlistService>();
            _controller = new WishlistController(_wishlistServiceMock.Object);
        }

        [Fact]
        public async Task GetWishlist_ReturnsOk_WithWishlistItems()
        {
            // Arrange
            var guestIds = new List<Guid> { Guid.NewGuid() };
            var mockResponse = new List<WishlistItemResponse>
            {
                new WishlistItemResponse(
                    Id: Guid.NewGuid(),
                    ProductVariantId: guestIds[0],
                    ProductId: Guid.NewGuid(),
                    ProductName: "Test Product",
                    ProductImageUrl: "url",
                    SKU: "SKU1",
                    Color: "Red",
                    Size: "M",
                    Price: 1000,
                    Stock: 10,
                    IsOutOfStock: false,
                    IsLowStock: false,
                    StockStatus: "InStock")
            };

            _wishlistServiceMock
                .Setup(s => s.GetWishlistAsync(guestIds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<WishlistItemResponse>>.Success(mockResponse));

            // Act
            var result = await _controller.GetWishlist(guestIds, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<WishlistItemResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().ContainSingle();
            apiResponse.Data![0].ProductName.Should().Be("Test Product");
        }

        [Fact]
        public async Task AddToWishlist_ReturnsOk_WithAddedItem()
        {
            // Arrange
            var request = new AddToWishlistRequest(Guid.NewGuid());
            var mockResponse = new WishlistItemResponse(
                Id: Guid.NewGuid(),
                ProductVariantId: request.ProductVariantId,
                ProductId: Guid.NewGuid(),
                ProductName: "Added Product",
                ProductImageUrl: "url",
                SKU: "SKU1",
                Color: "Blue",
                Size: "S",
                Price: 500,
                Stock: 5,
                IsOutOfStock: false,
                IsLowStock: false,
                StockStatus: "InStock");

            _wishlistServiceMock
                .Setup(s => s.AddToWishlistAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WishlistItemResponse>.Success(mockResponse));

            // Act
            var result = await _controller.AddToWishlist(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<WishlistItemResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.ProductName.Should().Be("Added Product");
        }

        [Fact]
        public async Task RemoveFromWishlist_ReturnsOk_WithTrue()
        {
            // Arrange
            var variantId = Guid.NewGuid();
            _wishlistServiceMock
                .Setup(s => s.RemoveFromWishlistAsync(variantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _controller.RemoveFromWishlist(variantId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task MergeWishlist_ReturnsOk_WithMergedItems()
        {
            // Arrange
            var request = new MergeWishlistRequest(new List<Guid> { Guid.NewGuid() });
            var mockResponse = new List<WishlistItemResponse>
            {
                new WishlistItemResponse(
                    Id: Guid.NewGuid(),
                    ProductVariantId: request.VariantIds[0],
                    ProductId: Guid.NewGuid(),
                    ProductName: "Merged Product",
                    ProductImageUrl: "url",
                    SKU: "SKUMerge",
                    Color: "Green",
                    Size: "L",
                    Price: 1500,
                    Stock: 15,
                    IsOutOfStock: false,
                    IsLowStock: false,
                    StockStatus: "InStock")
            };

            _wishlistServiceMock
                .Setup(s => s.MergeWishlistAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<WishlistItemResponse>>.Success(mockResponse));

            // Act
            var result = await _controller.MergeWishlist(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<WishlistItemResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data![0].ProductName.Should().Be("Merged Product");
        }
    }
}
