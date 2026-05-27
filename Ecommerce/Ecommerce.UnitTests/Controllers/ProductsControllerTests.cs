using Application.DTOs.Product;
using Application.Interfaces.Services;
using Application.Common.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _productServiceMock = new Mock<IProductService>();
            _controller = new ProductsController(_productServiceMock.Object);
        }

        private ProductResponse CreateDummyProductResponse(Guid id, string name)
        {
            return new ProductResponse(
                id,
                Guid.NewGuid(),
                name,
                "Description",
                "Cotton",
                100000,
                Domain.Enums.ProductStatus.Active,
                "Category",
                "http://image.png",
                100000,
                100000,
                10,
                "InStock",
                5.0,
                0,
                new List<Application.DTOs.Product.ProductVariants.ProductVariantResponse>()
            );
        }

        [Fact]
        public async Task GetProductById_ReturnsOk_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var productResponse = CreateDummyProductResponse(productId, "Test Product");
            var serviceResult = Result<ProductResponse>.Success(productResponse);

            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetProductById(productId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Id.Should().Be(productId);
            apiResponse.Data.Name.Should().Be("Test Product");
        }

        [Fact]
        public async Task GetProductById_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var serviceResult = Result<ProductResponse>.NotFound("Product not found");

            _productServiceMock
                .Setup(s => s.GetProductByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetProductById(productId, CancellationToken.None);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var apiResponse = notFoundResult.Value.Should().BeOfType<ApiResponse<ProductResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Product not found");
        }

        [Fact]
        public async Task CreateProduct_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new CreateProductRequest
            {
                Name = "New Product",
                Description = "Desc",
                BasePrice = 150000,
                CategoryId = Guid.NewGuid(),
                Status = Domain.Enums.ProductStatus.Active
            };
            var createdProduct = CreateDummyProductResponse(Guid.NewGuid(), "New Product");
            var serviceResult = Result<ProductResponse>.Success(createdProduct);

            _productServiceMock
                .Setup(s => s.CreateProductAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CreateProduct(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Name.Should().Be("New Product");
        }

        [Fact]
        public async Task DeleteProduct_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _productServiceMock
                .Setup(s => s.DeleteProductAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.DeleteProduct(productId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }

        [Fact]
        public async Task UploadProductImage_ReturnsBadRequest_WhenImageIsNull()
        {
            // Act
            var result = await _controller.UploadProductImage(Guid.NewGuid(), null, CancellationToken.None);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Errors.Should().Contain("Image file is required.");
        }

        [Fact]
        public async Task UploadProductImage_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var fileMock = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "image.png";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.ContentType).Returns("image/png");
            fileMock.Setup(_ => _.Length).Returns(ms.Length);

            var imageResponse = new ProductImageResponse
            {
                Id = Guid.NewGuid(),
                ImageUrl = "http://cloudinary.com/image.png"
            };
            var serviceResult = Result<ProductImageResponse>.Success(imageResponse);

            _productServiceMock
                .Setup(s => s.UploadProductImageAsync(
                    productId,
                    It.IsAny<Stream>(),
                    fileName,
                    "image/png",
                    ms.Length,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UploadProductImage(productId, fileMock.Object, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ProductImageResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.ImageUrl.Should().Be("http://cloudinary.com/image.png");
        }
    }
}
