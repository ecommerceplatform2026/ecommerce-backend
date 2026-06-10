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

        [Fact]
        public async Task GetProductReviews_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var request = new GetProductReviewsRequest();
            var pagedResult = new PagedResult<ReviewResponse>
            {
                Items = new System.Collections.Generic.List<ReviewResponse>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };
            var serviceResult = Result<PagedResult<ReviewResponse>>.Success(pagedResult);

            _reviewServiceMock
                .Setup(s => s.GetProductReviewsAsync(productId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetProductReviews(productId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ReviewResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task CheckReviewEligibility_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var eligibilityResponse = new ReviewEligibilityResponse(true, new System.Collections.Generic.List<EligibleOrderDto>());
            var serviceResult = Result<ReviewEligibilityResponse>.Success(eligibilityResponse);

            _reviewServiceMock
                .Setup(s => s.GetReviewEligibilityAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CheckReviewEligibility(productId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ReviewEligibilityResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.IsEligible.Should().BeTrue();
        }
    }
}
