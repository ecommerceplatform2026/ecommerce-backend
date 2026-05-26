using Application.DTOs.User.Address;
using Domain.Entities;
using Ecommerce.IntegrationTests.Helpers;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Responses;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.IntegrationTests.Controllers
{
    // Use a unique DB per class to avoid shared-state concurrency errors
    public class AddressesControllerIntegrationTests : IClassFixture<AddressesWebFactory>
    {
        private readonly AddressesWebFactory _factory;
        private readonly HttpClient _client;

        public AddressesControllerIntegrationTests(AddressesWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private void AuthenticateClient(HttpClient client, User user)
        {
            using var scope = _factory.Services.CreateScope();
            var jwtService = scope.ServiceProvider.GetRequiredService<Application.Interfaces.Services.IJwtService>();
            var token = jwtService.GenerateToken(user);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        [Fact]
        public async Task GetAddresses_ReturnsAddresses_WhenTheyExist()
        {
            // Arrange: seed user and address in a single scope (no Update needed)
            var user = User.Create("Address User 1", "addr1@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                // Add address BEFORE saving so it's tracked in the same scope
                user.AddAddress("Receiver 1", "0999888777", "123 Main St", "Ward 1", "District 1", "Province 1", true);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            // Act
            var response = await _client.GetAsync("/api/addresses");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AddressResponse>>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().Contain(a => a.ReceiverName == "Receiver 1" && a.IsDefault);
        }

        [Fact]
        public async Task CreateAddress_CreatesNewAddressSuccessfully()
        {
            // Arrange
            var user = User.Create("Address User 2", "addr2@example.com", "hash");
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EcommerceContext>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            AuthenticateClient(_client, user);

            var request = new CreateAddressRequest
            {
                ReceiverName = "Receiver 2",
                PhoneNumber = "0111222333",
                AddressLine = "789 Pine Rd",
                Ward = "Ward 3",
                District = "District 3",
                Province = "Province 3",
                IsDefault = true
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/addresses", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AddressResponse>>();
            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data!.ReceiverName.Should().Be("Receiver 2");
            result.Data.IsDefault.Should().BeTrue();
        }
    }

    /// <summary>Dedicated factory with isolated DB for AddressesController tests.</summary>
    public class AddressesWebFactory : CustomWebApplicationFactory
    {
        public AddressesWebFactory() : base($"AddressesDb_{Guid.NewGuid():N}") { }
    }
}
