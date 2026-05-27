using Application.DTOs.Product.ProductVariants;
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
    public class ProductVariantsControllerTests
    {
        private readonly Mock<IProductVariantService> _variantServiceMock;
        private readonly ProductVariantsController _controller;

        public ProductVariantsControllerTests()
        {
            _variantServiceMock = new Mock<IProductVariantService>();
            _controller = new ProductVariantsController(_variantServiceMock.Object);
        }

        private ProductVariantResponse CreateDummyVariantResponse(Guid productId, Guid variantId, string sku)
        {
            return new ProductVariantResponse(
                variantId,
                productId,
                sku,
                "Black",
                "M",
                50,
                5,
                120000,
                false,
                false
            );
        }

        [Fact]
        public async Task GetVariants_ReturnsOk_WithList()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var variants = new List<ProductVariantResponse>
            {
                CreateDummyVariantResponse(productId, Guid.NewGuid(), "SKU001"),
                CreateDummyVariantResponse(productId, Guid.NewGuid(), "SKU002")
            };
            var serviceResult = Result<List<ProductVariantResponse>>.Success(variants);

            _variantServiceMock
                .Setup(s => s.GetVariantsByProductIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetVariants(productId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ProductVariantResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetVariantById_ReturnsOk_WhenExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var variant = CreateDummyVariantResponse(productId, variantId, "SKU001");
            var serviceResult = Result<ProductVariantResponse>.Success(variant);

            _variantServiceMock
                .Setup(s => s.GetVariantByIdAsync(productId, variantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetVariantById(productId, variantId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductVariantResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Id.Should().Be(variantId);
        }

        [Fact]
        public async Task AddVariant_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var request = new CreateProductVariantRequest
            {
                SKU = "NEW_SKU",
                Color = "White",
                Size = "L",
                Stock = 100,
                LowStockThreshold = 10,
                Price = 130000
            };
            var createdVariant = CreateDummyVariantResponse(productId, Guid.NewGuid(), "NEW_SKU");
            var serviceResult = Result<ProductVariantResponse>.Success(createdVariant);

            _variantServiceMock
                .Setup(s => s.AddVariantAsync(productId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.AddVariant(productId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductVariantResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.SKU.Should().Be("NEW_SKU");
        }

        [Fact]
        public async Task UpdateVariant_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var request = new UpdateProductVariantRequest
            {
                SKU = "UPDATED_SKU",
                Color = "Blue",
                Size = "S",
                Price = 140000,
                LowStockThreshold = 5
            };
            var updatedVariant = CreateDummyVariantResponse(productId, variantId, "UPDATED_SKU");
            var serviceResult = Result<ProductVariantResponse>.Success(updatedVariant);

            _variantServiceMock
                .Setup(s => s.UpdateVariantAsync(productId, variantId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateVariant(productId, variantId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductVariantResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.SKU.Should().Be("UPDATED_SKU");
        }

        [Fact]
        public async Task DeleteVariant_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _variantServiceMock
                .Setup(s => s.DeleteVariantAsync(productId, variantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.DeleteVariant(productId, variantId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateStock_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _variantServiceMock
                .Setup(s => s.UpdateStockAsync(productId, variantId, 75, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateStock(productId, variantId, 75, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }
    }
}
