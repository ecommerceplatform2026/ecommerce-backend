using Application.DTOs.User;
using Application.Interfaces.Services;
using Application.Common.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Common.Responses;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _controller = new UserController(_userServiceMock.Object);
        }

        [Fact]
        public async Task GetUser_ReturnsOk_WithUserProfile()
        {
            // Arrange
            var userResponse = new UserResponse
            {
                Id = Guid.NewGuid(),
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "123456789"
            };
            var serviceResult = Result<UserResponse>.Success(userResponse);

            _userServiceMock
                .Setup(s => s.GetUserAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetUser(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.FullName.Should().Be("Test User");
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new UpdateUserRequest
            {
                FullName = "Updated Name",
                PhoneNumber = "987654321"
            };
            var userResponse = new UserResponse
            {
                Id = Guid.NewGuid(),
                FullName = "Updated Name",
                Email = "test@example.com",
                PhoneNumber = "987654321"
            };
            var serviceResult = Result<UserResponse>.Success(userResponse);

            _userServiceMock
                .Setup(s => s.UpdateUserAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateUser(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.FullName.Should().Be("Updated Name");
        }
    }
}
