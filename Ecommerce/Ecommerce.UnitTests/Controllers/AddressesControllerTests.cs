using Application.DTOs.User.Address;
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
    public class AddressesControllerTests
    {
        private readonly Mock<IAddressService> _addressServiceMock;
        private readonly AddressesController _controller;

        public AddressesControllerTests()
        {
            _addressServiceMock = new Mock<IAddressService>();
            _controller = new AddressesController(_addressServiceMock.Object);
        }

        private AddressResponse CreateDummyAddressResponse(Guid id, string receiverName)
        {
            return new AddressResponse
            {
                Id = id,
                ReceiverName = receiverName,
                PhoneNumber = "123456789",
                AddressLine = "123 Main St",
                Ward = "Ward 1",
                District = "District 1",
                Province = "Province 1",
                IsDefault = true
            };
        }

        [Fact]
        public async Task GetAddresses_ReturnsOk_WithList()
        {
            // Arrange
            var addresses = new List<AddressResponse>
            {
                CreateDummyAddressResponse(Guid.NewGuid(), "John Doe"),
                CreateDummyAddressResponse(Guid.NewGuid(), "Jane Doe")
            };
            var serviceResult = Result<List<AddressResponse>>.Success(addresses);

            _addressServiceMock
                .Setup(s => s.GetAddressesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAddresses(CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<AddressResponse>>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAddressById_ReturnsOk_WhenExists()
        {
            // Arrange
            var addressId = Guid.NewGuid();
            var address = CreateDummyAddressResponse(addressId, "John Doe");
            var serviceResult = Result<AddressResponse>.Success(address);

            _addressServiceMock
                .Setup(s => s.GetAddressByIdAsync(addressId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.GetAddressById(addressId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AddressResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.Id.Should().Be(addressId);
        }

        [Fact]
        public async Task CreateAddress_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var request = new CreateAddressRequest
            {
                ReceiverName = "Jane Doe",
                PhoneNumber = "987654321",
                AddressLine = "456 Oak Rd",
                Ward = "Ward 2",
                District = "District 2",
                Province = "Province 2",
                IsDefault = false
            };
            var response = CreateDummyAddressResponse(Guid.NewGuid(), "Jane Doe");
            response.IsDefault = false;
            var serviceResult = Result<AddressResponse>.Success(response);

            _addressServiceMock
                .Setup(s => s.CreateAddressAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.CreateAddress(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AddressResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.ReceiverName.Should().Be("Jane Doe");
        }

        [Fact]
        public async Task UpdateAddress_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var addressId = Guid.NewGuid();
            var request = new UpdateAddressRequest
            {
                ReceiverName = "Jane Doe Updated",
                PhoneNumber = "987654321",
                AddressLine = "456 Oak Rd",
                Ward = "Ward 2",
                District = "District 2",
                Province = "Province 2",
                IsDefault = true
            };
            var response = CreateDummyAddressResponse(addressId, "Jane Doe Updated");
            var serviceResult = Result<AddressResponse>.Success(response);

            _addressServiceMock
                .Setup(s => s.UpdateAddressAsync(addressId, request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.UpdateAddress(addressId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<AddressResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data!.ReceiverName.Should().Be("Jane Doe Updated");
        }

        [Fact]
        public async Task DeleteAddress_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var addressId = Guid.NewGuid();
            var serviceResult = Result<bool>.Success(true);

            _addressServiceMock
                .Setup(s => s.DeleteAddressAsync(addressId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _controller.DeleteAddress(addressId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<bool>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().BeTrue();
        }
    }
}
