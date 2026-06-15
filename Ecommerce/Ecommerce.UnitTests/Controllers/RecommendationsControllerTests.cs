using Application.Common.Response;
using Application.DTOs.Product;
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
    public class RecommendationsControllerTests
    {
        private readonly Mock<IRecommendationService> _recommendationServiceMock;
        private readonly RecommendationsController _controller;

        public RecommendationsControllerTests()
        {
            _recommendationServiceMock = new Mock<IRecommendationService>();
            _controller = new RecommendationsController(_recommendationServiceMock.Object);
        }

        [Fact]
        public async Task GetPopularProducts_ReturnsOk_WithPopularProducts()
        {
            // Arrange
            var mockResponse = new List<ProductResponse>
            {
                new ProductResponse(
                    Id: Guid.NewGuid(),
                    CategoryId: Guid.NewGuid(),
                    Name: "Popular Product",
                    Description: "Desc",
                    Material: "Material",
                    BasePrice: 100000,
                    Status: Domain.Enums.ProductStatus.Active,
                    CategoryName: "Category A",
                    ImageUrl: "url",
                    MinPrice: 100000,
                    MaxPrice: 100000,
                    TotalStock: 100,
                    StockStatus: "InStock",
                    AverageRating: 4.5,
                    ReviewCount: 10,
                    Variants: new List<Application.DTOs.Product.ProductVariants.ProductVariantResponse>()
                )
            };

            _recommendationServiceMock
                .Setup(s => s.GetPopularProductsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<ProductResponse>>.Success(mockResponse));

            // Act
            var result = await _controller.GetPopularProducts(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ProductResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().ContainSingle();
            apiResponse.Data![0].Name.Should().Be("Popular Product");
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_ReturnsOk_WithPersonalizedProducts()
        {
            // Arrange
            var mockResponse = new List<ProductResponse>
            {
                new ProductResponse(
                    Id: Guid.NewGuid(),
                    CategoryId: Guid.NewGuid(),
                    Name: "Personalized Product",
                    Description: "Desc",
                    Material: "Material",
                    BasePrice: 200000,
                    Status: Domain.Enums.ProductStatus.Active,
                    CategoryName: "Category B",
                    ImageUrl: "url",
                    MinPrice: 200000,
                    MaxPrice: 200000,
                    TotalStock: 50,
                    StockStatus: "InStock",
                    AverageRating: 4.8,
                    ReviewCount: 20,
                    Variants: new List<Application.DTOs.Product.ProductVariants.ProductVariantResponse>()
                )
            };

            _recommendationServiceMock
                .Setup(s => s.GetPersonalizedRecommendationsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<ProductResponse>>.Success(mockResponse));

            // Act
            var result = await _controller.GetPersonalizedRecommendations(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ProductResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().ContainSingle();
            apiResponse.Data![0].Name.Should().Be("Personalized Product");
        }

        [Fact]
        public async Task GetSimilarProducts_ReturnsOk_WithSimilarProducts()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var mockResponse = new List<ProductResponse>
            {
                new ProductResponse(
                    Id: Guid.NewGuid(),
                    CategoryId: Guid.NewGuid(),
                    Name: "Similar Product",
                    Description: "Desc",
                    Material: "Material",
                    BasePrice: 150000,
                    Status: Domain.Enums.ProductStatus.Active,
                    CategoryName: "Category C",
                    ImageUrl: "url",
                    MinPrice: 150000,
                    MaxPrice: 150000,
                    TotalStock: 30,
                    StockStatus: "InStock",
                    AverageRating: 4.2,
                    ReviewCount: 5,
                    Variants: new List<Application.DTOs.Product.ProductVariants.ProductVariantResponse>()
                )
            };

            _recommendationServiceMock
                .Setup(s => s.GetSimilarProductsAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<ProductResponse>>.Success(mockResponse));

            // Act
            var result = await _controller.GetSimilarProducts(productId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<ProductResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().ContainSingle();
            apiResponse.Data![0].Name.Should().Be("Similar Product");
        }
    }
}
