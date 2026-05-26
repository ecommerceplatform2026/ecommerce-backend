using Application.DTOs.Review;
using Application.Interfaces.Services;
using Application.Common.Response;
using Domain.Enums;
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
    public class ReviewsControllerTests
    {
        private readonly Mock<IReviewService> _reviewServiceMock;
        private readonly ReviewsController _controller;

        public ReviewsControllerTests()
        {
            _reviewServiceMock = new Mock<IReviewService>();
            _controller = new ReviewsController(_reviewServiceMock.Object);
        }

        [Fact]
        public async Task CreateReview_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var request = new CreateReviewRequest(
                ProductId: productId,
                OrderId: orderId,
                Rating: 5,
                Title: "Excellent",
                Comment: "Excellent quality!"
            );

            var reviewResponse = new ReviewResponse(
                Guid.NewGuid(),
                Guid.NewGuid(),
                productId,
                orderId,
                5,
                "Excellent",
                "Excellent quality!",
                ReviewStatus.Approved,
                DateTime.UtcNow
            );
            var serviceResult = Result<ReviewResponse>.Success(reviewResponse);

            _reviewServiceMock
                .Setup(s => s.CreateReviewAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CreateReview(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReviewResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Comment.Should().Be("Excellent quality!");
        }
    }
}
