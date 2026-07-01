using Application.Common.Response;
using Application.DTOs.Product;
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Ecommerce.UnitTests.Services
{
    public class RecommendationServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ICurrentUserService> _currentUserServiceMock;
        private readonly Mock<IGenericRepository<Product>> _productRepositoryMock;
        private readonly Mock<IGenericRepository<RecentlyViewedProduct>> _recentlyViewedRepositoryMock;
        private readonly Mock<IGenericRepository<OrderItem>> _orderItemRepositoryMock;
        private readonly RecommendationService _service;

        public RecommendationServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheServiceMock = new Mock<ICacheService>();
            _currentUserServiceMock = new Mock<ICurrentUserService>();
            
            _productRepositoryMock = new Mock<IGenericRepository<Product>>();
            _recentlyViewedRepositoryMock = new Mock<IGenericRepository<RecentlyViewedProduct>>();
            _orderItemRepositoryMock = new Mock<IGenericRepository<OrderItem>>();

            _unitOfWorkMock.Setup(u => u.GetRepository<Product>()).Returns(_productRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<RecentlyViewedProduct>()).Returns(_recentlyViewedRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.GetRepository<OrderItem>()).Returns(_orderItemRepositoryMock.Object);

            _service = new RecommendationService(
                _unitOfWorkMock.Object,
                _cacheServiceMock.Object,
                _currentUserServiceMock.Object
            );

            // Mock default Cache Behaviour: return raw value (no caching layer interference)
            _cacheServiceMock
                .Setup(c => c.GetOrAddAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<List<ProductResponse>?>>>(),
                    It.IsAny<TimeSpan?>(),
                    It.IsAny<CancellationToken>()
                ))
                .Returns<string, Func<Task<List<ProductResponse>?>>, TimeSpan?, CancellationToken>(
                    async (key, factory, ttl, token) => await factory()
                );

            // Mock default OrderItem repository behavior
            _orderItemRepositoryMock
                .Setup(r => r.GetAllAsync(
                    It.IsAny<Expression<Func<OrderItem, bool>>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Expression<Func<OrderItem, object>>[]>()))
                .ReturnsAsync(new List<OrderItem>());
        }

        private void SetCurrentUser(Guid? userId, string? role)
        {
            _currentUserServiceMock.Setup(s => s.GetUserIdOrNull()).Returns(userId?.ToString());
            _currentUserServiceMock.Setup(s => s.GetUserRoleOrNull()).Returns(role);
        }

        private static Product CreateProduct(Guid id, string name, long basePrice, Guid categoryId, int stock = 10, DateTime? createdAt = null)
        {
            var category = Category.Create(name + " Cat");
            var categoryIdProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
            categoryIdProp?.SetValue(category, categoryId);

            var product = Product.Create(categoryId, name, "desc", "material", new Money(basePrice), ProductStatus.Active);
            product.Category = category;

            var idProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
            idProp?.SetValue(product, id);

            if (createdAt.HasValue)
            {
                var createdProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt));
                createdProp?.SetValue(product, createdAt.Value);
            }

            if (stock > 0)
            {
                product.AddVariant("SKU-" + name, "Color", "Size", stock, new Money(basePrice), 5);
            }
            else
            {
                // out of stock variant
                product.AddVariant("SKU-" + name, "Color", "Size", 0, new Money(basePrice), 5);
            }

            return product;
        }

        private static OrderItem CreateOrderItem(Guid productId, int quantity, DateTime orderCreatedAt)
        {
            var product = CreateProduct(productId, "Product-" + productId, 100000, Guid.NewGuid());
            var variant = product.ProductVariants.First();

            var mockOrder = Order.Create(Guid.NewGuid(), 12345, PaymentMethod.COD);
            var orderIdProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id));
            orderIdProp?.SetValue(mockOrder, Guid.NewGuid());

            var orderCreatedProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.CreatedAt));
            orderCreatedProp?.SetValue(mockOrder, orderCreatedAt);

            // Confirm order to make status non-pending
            mockOrder.MarkAsConfirmed();

            mockOrder.AddItem(variant.Id, quantity, new Money(100000), "Snapshot");
            var orderItem = mockOrder.OrderItems.First();
            orderItem.Order = mockOrder;
            variant.Product = product;
            orderItem.ProductVariant = variant;

            return orderItem;
        }

        [Fact]
        public async Task TrackProductViewAsync_WhenUserNotLoggedIn_ReturnsFalse()
        {
            SetCurrentUser(null, null);

            var result = await _service.TrackProductViewAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeFalse();
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecentlyViewedProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TrackProductViewAsync_WhenUserIsAdmin_ReturnsFalse()
        {
            SetCurrentUser(Guid.NewGuid(), "Admin");

            var result = await _service.TrackProductViewAsync(Guid.NewGuid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeFalse();
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecentlyViewedProduct>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task TrackProductViewAsync_WhenUserIsUser_AndProductExists_AddsViewHistory()
        {
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            SetCurrentUser(userId, "User");

            _unitOfWorkMock
                .Setup(u => u.GetRepository<Product>().TotalAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(1); // product exists

            _recentlyViewedRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<RecentlyViewedProduct, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<RecentlyViewedProduct, object>>[]>()))
                .ReturnsAsync((RecentlyViewedProduct?)null);

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetAllTrackedAsync(It.IsAny<Expression<Func<RecentlyViewedProduct, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<RecentlyViewedProduct, object>>[]>()))
                .ReturnsAsync(new List<RecentlyViewedProduct>());

            var result = await _service.TrackProductViewAsync(productId, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.IsAny<RecentlyViewedProduct>(), It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPopularProductsAsync_ReturnsProductsRankedBySalesVolume()
        {
            var p1 = CreateProduct(Guid.NewGuid(), "Product A", 100000, Guid.NewGuid(), 10);
            var p2 = CreateProduct(Guid.NewGuid(), "Product B", 200000, Guid.NewGuid(), 10);
            var p3 = CreateProduct(Guid.NewGuid(), "Product C (Out of Stock)", 150000, Guid.NewGuid(), 0); // Out of stock

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { p1, p2, p3 });

            // Mock sales data: Product B sold 10 units, Product A sold 5 units, Product C sold 20 units
            var oi1 = CreateOrderItem(p1.Id, 5, DateTime.UtcNow);
            var oi2 = CreateOrderItem(p2.Id, 10, DateTime.UtcNow);
            var oi3 = CreateOrderItem(p3.Id, 20, DateTime.UtcNow);

            _orderItemRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OrderItem, object>>[]>()))
                .ReturnsAsync(new List<OrderItem> { oi1, oi2, oi3 });

            var result = await _service.GetPopularProductsAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Product C must be excluded because it is out of stock (AC2)
            result.Value.Should().HaveCount(2);
            // Product B must be first because it has 10 units sold vs 5 units for Product A (AC1)
            result.Value[0].Id.Should().Be(p2.Id);
            result.Value[1].Id.Should().Be(p1.Id);
        }

        [Fact]
        public async Task GetPopularProductsAsync_FillsWithNewestArrivals_WhenFewerThan20Sold()
        {
            var catId = Guid.NewGuid();
            var products = new List<Product>();
            for (int i = 1; i <= 25; i++)
            {
                products.Add(CreateProduct(Guid.NewGuid(), $"Product-{i}", 100000, catId, 10, DateTime.UtcNow.AddMinutes(-i)));
            }

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(products);

            // Mock 1 sale for Product-5
            var oi1 = CreateOrderItem(products[4].Id, 1, DateTime.UtcNow);
            _orderItemRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OrderItem, object>>[]>()))
                .ReturnsAsync(new List<OrderItem> { oi1 });

            var result = await _service.GetPopularProductsAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(20);
            
            // First product must be Product-5 (the only one with sales)
            result.Value[0].Id.Should().Be(products[4].Id);
            
            // The rest should follow newest arrivals ordering
            result.Value[1].Id.Should().Be(products[0].Id); // newest overall
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_WhenGuest_ReturnsColdStartFallback()
        {
            SetCurrentUser(null, null);

            var p1 = CreateProduct(Guid.NewGuid(), "Prod-1", 100000, Guid.NewGuid(), 10, DateTime.UtcNow);
            var p2 = CreateProduct(Guid.NewGuid(), "Prod-2", 100000, Guid.NewGuid(), 10, DateTime.UtcNow.AddMinutes(-5));

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { p1, p2 });

            var result = await _service.GetPersonalizedRecommendationsAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().HaveCount(2);
            result.Value[0].Id.Should().Be(p1.Id); // sorted by newest arrival
        }

        [Fact]
        public async Task GetPersonalizedRecommendationsAsync_ExcludesSeenAndPurchasedItems()
        {
            var userId = Guid.NewGuid();
            SetCurrentUser(userId, "User");

            var catId = Guid.NewGuid();
            var p1 = CreateProduct(Guid.NewGuid(), "Seen Product", 100000, catId);
            var p2 = CreateProduct(Guid.NewGuid(), "Purchased Product", 120000, catId);
            var p3 = CreateProduct(Guid.NewGuid(), "Recommended Product", 110000, catId);

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { p1, p2, p3 });

            // Setup view history
            var view = RecentlyViewedProduct.Create(userId, p1.Id);
            view.Product = p1;
            _recentlyViewedRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<RecentlyViewedProduct, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<RecentlyViewedProduct, object>>[]>()))
                .ReturnsAsync(new List<RecentlyViewedProduct> { view });

            // Setup purchase history
            var purchase = CreateOrderItem(p2.Id, 1, DateTime.UtcNow);
            purchase.Order!.SetCreated(userId.ToString()); // mocked userId via string
            // Reflection to override private UserId in Order
            var userIdProp = typeof(Order).GetProperty(nameof(Order.UserId));
            userIdProp?.SetValue(purchase.Order, userId);

            _orderItemRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<OrderItem, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<OrderItem, object>>[]>()))
                .ReturnsAsync(new List<OrderItem> { purchase });

            var result = await _service.GetPersonalizedRecommendationsAsync(CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Should exclude p1 and p2, returning only p3 (AC7)
            result.Value.Should().ContainSingle();
            result.Value[0].Id.Should().Be(p3.Id);
        }

        [Fact]
        public async Task GetSimilarProductsAsync_ReturnsSameCategory_InNarrowPriceRange()
        {
            var catId = Guid.NewGuid();
            var baseProduct = CreateProduct(Guid.NewGuid(), "Base", 100000, catId); // Base price: 100,000 VND
            
            // Similar products
            var p1 = CreateProduct(Guid.NewGuid(), "Similar 1", 110000, catId); // inside 30% (110k)
            var p2 = CreateProduct(Guid.NewGuid(), "Similar 2", 90000, catId);  // inside 30% (90k)
            var p3 = CreateProduct(Guid.NewGuid(), "Similar 3", 80000, catId);  // inside 30% (80k)
            var p4 = CreateProduct(Guid.NewGuid(), "Similar 4", 120000, catId); // inside 30% (120k)
            var p5 = CreateProduct(Guid.NewGuid(), "Out of Range", 160000, catId); // outside 30% (160k)

            _productRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(baseProduct);

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { p1, p2, p3, p4, p5 });

            var result = await _service.GetSimilarProductsAsync(baseProduct.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Should contain 4 similar products (since narrowMatches.Count = 4 >= 4)
            result.Value.Should().HaveCount(4);
            result.Value.Select(r => r.Id).Should().NotContain(p5.Id);
        }

        [Fact]
        public async Task GetSimilarProductsAsync_WidensToWideRange_WhenFewerThan4SimilarProductsInNarrowRange()
        {
            var catId = Guid.NewGuid();
            var baseProduct = CreateProduct(Guid.NewGuid(), "Base", 100000, catId); // Base price: 100,000 VND

            // Similar products
            var p1 = CreateProduct(Guid.NewGuid(), "Similar 1", 110000, catId); // inside 30% (110k)
            var p2 = CreateProduct(Guid.NewGuid(), "Similar 2", 145000, catId); // outside 30%, inside 50% (145k)
            var p3 = CreateProduct(Guid.NewGuid(), "Similar 3", 60000, catId);  // outside 30%, inside 50% (60k)
            var p4 = CreateProduct(Guid.NewGuid(), "Similar 4", 140000, catId); // outside 30%, inside 50% (140k)

            _productRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(baseProduct);

            _productRepositoryMock
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Product, object>>[]>()))
                .ReturnsAsync(new List<Product> { p1, p2, p3, p4 });

            var result = await _service.GetSimilarProductsAsync(baseProduct.Id, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            // Since only 1 product (p1) is in narrow range, we widen to ±50% range.
            // Under ±50%, all 4 (p1, p2, p3, p4) should be matched (AC12)
            result.Value.Should().HaveCount(4);
            result.Value.Select(r => r.Id).Should().Contain(new[] { p1.Id, p2.Id, p3.Id, p4.Id });
        }
    }
}
