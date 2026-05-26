using Application.DTOs.Auth;
using Application.Interfaces.Services;
using Application.Common.Response;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Presentation.Controllers;
using Presentation.Common.Responses;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _controller = new AuthController(_authServiceMock.Object);
        }

        [Fact]
        public async Task Register_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "Password123"
            };
            var authResponse = new AuthResponse
            {
                Token = "jwt_token",
                FullName = "Test User",
                Email = "test@example.com",
                Role = "Customer"
            };
            var serviceResult = Result<AuthResponse>.Success(authResponse);

            _authServiceMock
                .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Token.Should().Be("jwt_token");
            apiResponse.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Register_WithFailedRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = new RegisterRequest
            {
                FullName = "Test User",
                Email = "test@example.com",
                Password = "Password123"
            };
            var serviceResult = Result<AuthResponse>.Failure("Email already exists");

            _authServiceMock
                .Setup(s => s.RegisterAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Register(request, CancellationToken.None);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var apiResponse = badRequestResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Data.Should().BeNull();
            apiResponse.Errors.Should().Contain("Email already exists");
        }

        [Fact]
        public async Task Login_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };
            var authResponse = new AuthResponse
            {
                Token = "jwt_token",
                FullName = "Test User",
                Email = "test@example.com",
                Role = "Customer"
            };
            var serviceResult = Result<AuthResponse>.Success(authResponse);

            _authServiceMock
                .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.Token.Should().Be("jwt_token");
            apiResponse.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };
            var serviceResult = Result<AuthResponse>.Unauthorized("Invalid username or password");

            _authServiceMock
                .Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.Login(request, CancellationToken.None);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            var apiResponse = unauthorizedResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
            apiResponse.Success.Should().BeFalse();
            apiResponse.Data.Should().BeNull();
            apiResponse.Errors.Should().Contain("Invalid username or password");
        }
    }
}
