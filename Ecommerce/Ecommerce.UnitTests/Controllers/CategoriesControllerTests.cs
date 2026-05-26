using Application.DTOs.Category;
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
    public class CategoriesControllerTests
    {
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _controller = new CategoriesController(_categoryServiceMock.Object);
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithList()
        {
            // Arrange
            var categories = new List<CategoryResponse>
            {
                new CategoryResponse(Guid.NewGuid(), "Dresses", DateTime.UtcNow),
                new CategoryResponse(Guid.NewGuid(), "Shirts", DateTime.UtcNow)
            };
            var serviceResult = Result<List<CategoryResponse>>.Success(categories);

            _categoryServiceMock
                .Setup(s => s.GetCategoriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetCategories(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<CategoryResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateCategory_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new CreateCategoryRequest { Name = "Shoes" };
            var response = new CategoryResponse(Guid.NewGuid(), "Shoes", DateTime.UtcNow);
            var serviceResult = Result<CategoryResponse>.Success(response);

            _categoryServiceMock
                .Setup(s => s.CreateCategoryAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CreateCategory(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CategoryResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Name.Should().Be("Shoes");
        }

        [Fact]
        public async Task UpdateCategory_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var request = new UpdateCategoryRequest { Name = "Updated Shoes" };
            var response = new CategoryResponse(categoryId, "Updated Shoes", DateTime.UtcNow);
            var serviceResult = Result<CategoryResponse>.Success(response);

            _categoryServiceMock
                .Setup(s => s.UpdateCategoryAsync(categoryId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateCategory(categoryId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<CategoryResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Name.Should().Be("Updated Shoes");
        }

        [Fact]
        public async Task DeleteCategory_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _categoryServiceMock
                .Setup(s => s.DeleteCategoryAsync(categoryId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.DeleteCategory(categoryId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }
    }
}
