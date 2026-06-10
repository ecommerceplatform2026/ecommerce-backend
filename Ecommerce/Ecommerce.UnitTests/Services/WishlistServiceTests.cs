using Application.Common.Response;
using Application.DTOs.Wishlist;
using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Services
{
    public class WishlistServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IGenericRepository<WishlistItem>> _wishlistRepositoryMock;
        private readonly Mock<IGenericRepository<ProductVariant>> _variantRepositoryMock;
        private readonly WishlistService _service;

        public WishlistServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            _wishlistRepositoryMock = new Mock<IGenericRepository<WishlistItem>>();
            _variantRepositoryMock = new Mock<IGenericRepository<ProductVariant>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<WishlistItem>()).Returns(_wishlistRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<ProductVariant>()).Returns(_variantRepositoryMock.Object);

            _service = new WishlistService(_unitOfWorkMock.Object, _currentUserServiceMock.Object);
        }

        private void SetupCurrentUser(Guid? userId)
        {
            _currentUserServiceMock
                .Setup(s => s.GetUserIdOrNull())
                .Returns(userId?.ToString());
        }

        private void SetupProductVariant(Guid variantId, ProductVariant? variant)
        {
            _variantRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<ProductVariant, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<ProductVariant, object>>[]>()))
                .ReturnsAsync(variant);
        }

        private void SetupWishlistItem(WishlistItem? wishlistItem)
        {
            _wishlistRepositoryMock
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<WishlistItem, object>>[]>()))
                .ReturnsAsync(wishlistItem);
        }

        private void SetupWishlistItemsList(List<WishlistItem> list)
        {
            _wishlistRepositoryMock
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<WishlistItem, object>>[]>()))
                .ReturnsAsync(list);
        }

        private static ProductVariant CreateActiveVariant(Guid? variantId = null)
        {
            var product = Product.Create(Guid.NewGuid(), "Test Product", "Desc", "Material", new Money(120000), ProductStatus.Active);
            var variant = ProductVariant.Create(product.Id, new Sku("SKU-123"), "Red", "M", 25, new Money(120000));
            variant.Product = product;
            
            if (variantId.HasValue)
            {
                var idProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
                idProp?.SetValue(variant, variantId.Value);
            }

            return variant;
        }

        [Fact]
        public async Task GetWishlistAsync_WhenUserNotLoggedIn_AndNoGuestIdsProvided_ReturnsEmptyList()
        {
            SetupCurrentUser(null);

            var result = await _service.GetWishlistAsync(null, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GetWishlistAsync_WhenUserNotLoggedIn_AndGuestIdsProvided_ReturnsMappedVariantDetails()
        {
            SetupCurrentUser(null);
            var variantId = Guid.NewGuid();
            var variant = CreateActiveVariant(variantId);
            SetupProductVariant(variantId, variant);

            var result = await _service.GetWishlistAsync(new List<Guid> { variantId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value[0].ProductVariantId.Should().Be(variantId);
            result.Value[0].ProductName.Should().Be("Test Product");
            result.Value[0].Id.Should().Be(Guid.Empty);
        }

        [Fact]
        public async Task GetWishlistAsync_WhenUserLoggedIn_AndNoWishlistItems_ReturnsEmptyList()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            SetupWishlistItemsList(new List<WishlistItem>());

            var result = await _service.GetWishlistAsync(null, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }

        [Fact]
        public async Task GetWishlistAsync_WhenUserLoggedIn_AndHasWishlistItems_ReturnsMappedList()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            var variant = CreateActiveVariant();
            var wishlistItem = WishlistItem.Create(userId, variant.Id);
            wishlistItem.ProductVariant = variant;
            SetupWishlistItemsList(new List<WishlistItem> { wishlistItem });

            var result = await _service.GetWishlistAsync(null, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value[0].ProductVariantId.Should().Be(variant.Id);
            result.Value[0].ProductName.Should().Be("Test Product");
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenRequestNull_ReturnsFailure()
        {
            var result = await _service.AddToWishlistAsync(null!, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Request cannot be null.");
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenVariantNotFound_ReturnsNotFound()
        {
            var request = new AddToWishlistRequest(Guid.NewGuid());
            SetupProductVariant(request.ProductVariantId, null);

            var result = await _service.AddToWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Product variant not found.");
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenProductInactive_ReturnsFailure()
        {
            var request = new AddToWishlistRequest(Guid.NewGuid());
            var product = Product.Create(Guid.NewGuid(), "Inactive Product", "Desc", "Material", new Money(100), ProductStatus.Inactive);
            var variant = ProductVariant.Create(product.Id, new Sku("SKU-Inactive"), "Blue", "L", 5, new Money(100));
            variant.Product = product;
            SetupProductVariant(request.ProductVariantId, variant);

            var result = await _service.AddToWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Product is inactive or unavailable.");
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenUserNotLoggedIn_PreparesAndReturnsGuestDataWithoutDBSave()
        {
            var request = new AddToWishlistRequest(Guid.NewGuid());
            var variant = CreateActiveVariant(request.ProductVariantId);
            SetupProductVariant(request.ProductVariantId, variant);
            SetupCurrentUser(null);

            var result = await _service.AddToWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.ProductVariantId.Should().Be(request.ProductVariantId);
            result.Value.Id.Should().Be(Guid.Empty);
            _wishlistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenUserLoggedIn_AndItemAlreadyExists_PreventsDuplicateAddition()
        {
            var request = new AddToWishlistRequest(Guid.NewGuid());
            var variant = CreateActiveVariant(request.ProductVariantId);
            SetupProductVariant(request.ProductVariantId, variant);
            
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);

            var existingItem = WishlistItem.Create(userId, request.ProductVariantId);
            existingItem.ProductVariant = variant;
            SetupWishlistItem(existingItem);

            var result = await _service.AddToWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.ProductVariantId.Should().Be(request.ProductVariantId);
            _wishlistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenUserLoggedIn_AndItemDoesNotExist_AddsToWishlist()
        {
            var request = new AddToWishlistRequest(Guid.NewGuid());
            var variant = CreateActiveVariant(request.ProductVariantId);
            SetupProductVariant(request.ProductVariantId, variant);

            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);

            // First FindAsync returns null (doesn't exist)
            // Second FindAsync returns the newly created item
            var newItem = WishlistItem.Create(userId, request.ProductVariantId);
            newItem.ProductVariant = variant;

            _wishlistRepositoryMock
                .SetupSequence(r => r.FindAsync(
                    It.IsAny<Expression<Func<WishlistItem, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<WishlistItem, object>>[]>()))
                .ReturnsAsync((WishlistItem?)null)
                .ReturnsAsync(newItem);

            var result = await _service.AddToWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value!.ProductVariantId.Should().Be(request.ProductVariantId);
            _wishlistRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WishlistItem>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveFromWishlistAsync_WhenUserNotLoggedIn_ReturnsSuccessWithoutDBSave()
        {
            SetupCurrentUser(null);

            var result = await _service.RemoveFromWishlistAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            _wishlistRepositoryMock.Verify(r => r.Remove(It.IsAny<WishlistItem>()), Times.Never);
        }

        [Fact]
        public async Task RemoveFromWishlistAsync_WhenUserLoggedIn_AndItemNotFound_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);
            SetupWishlistItem(null);

            var result = await _service.RemoveFromWishlistAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Wishlist item not found in wishlist.");
        }

        [Fact]
        public async Task RemoveFromWishlistAsync_WhenUserLoggedIn_AndItemExists_RemovesFromWishlist()
        {
            var userId = Guid.NewGuid();
            var variantId = Guid.NewGuid();
            SetupCurrentUser(userId);

            var item = WishlistItem.Create(userId, variantId);
            SetupWishlistItem(item);

            var result = await _service.RemoveFromWishlistAsync(variantId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            _wishlistRepositoryMock.Verify(r => r.Remove(item), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MergeWishlistAsync_WhenRequestNull_ReturnsFailure()
        {
            var result = await _service.MergeWishlistAsync(null!, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Request cannot be null.");
        }

        [Fact]
        public async Task MergeWishlistAsync_WhenUserNotLoggedIn_ReturnsUnauthorized()
        {
            SetupCurrentUser(null);
            var request = new MergeWishlistRequest(new List<Guid> { Guid.NewGuid() });

            var result = await _service.MergeWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Guest user cannot merge wishlist.");
        }

        [Fact]
        public async Task MergeWishlistAsync_WhenUserLoggedIn_AndRequestContainsItems_MergesNonDuplicates()
        {
            var userId = Guid.NewGuid();
            SetupCurrentUser(userId);

            var existingVariantId = Guid.NewGuid();
            var newVariantId = Guid.NewGuid();

            var existingItem = WishlistItem.Create(userId, existingVariantId);
            SetupWishlistItemsList(new List<WishlistItem> { existingItem });

            var newVariant = CreateActiveVariant(newVariantId);
            SetupProductVariant(newVariantId, newVariant);

            var request = new MergeWishlistRequest(new List<Guid> { existingVariantId, newVariantId });

            var result = await _service.MergeWishlistAsync(request, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _wishlistRepositoryMock.Verify(r => r.AddAsync(It.Is<WishlistItem>(w => w.ProductVariantId == newVariantId), It.IsAny<CancellationToken>()), Times.Once);
            _wishlistRepositoryMock.Verify(r => r.AddAsync(It.Is<WishlistItem>(w => w.ProductVariantId == existingVariantId), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
