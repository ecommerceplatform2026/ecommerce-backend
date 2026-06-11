using Ecommerce.PerformanceTests.Scenarios;
using NBomber.CSharp;

namespace Ecommerce.PerformanceTests;

[Collection("PerformanceTests")]
public sealed class AuthLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AuthLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AuthController_LoadTest() { NBomberRunner.RegisterScenarios(new AuthControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/auth-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductsLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ProductsLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ProductsController_LoadTest() { NBomberRunner.RegisterScenarios(new ProductsControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/products-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CategoriesLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CategoriesLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CategoriesController_LoadTest() { NBomberRunner.RegisterScenarios(new CategoriesControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/categories-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductVariantsLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ProductVariantsLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ProductVariantsController_LoadTest() { NBomberRunner.RegisterScenarios(new ProductVariantsControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/product-variants-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CartLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CartLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CartController_LoadTest() { NBomberRunner.RegisterScenarios(new CartControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/cart-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CheckoutLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CheckoutLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CheckoutController_LoadTest() { NBomberRunner.RegisterScenarios(new CheckoutControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/checkout-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class OrdersLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public OrdersLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void OrdersController_LoadTest() { NBomberRunner.RegisterScenarios(new OrdersControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/orders-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class PaymentsLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public PaymentsLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void PaymentsController_LoadTest() { NBomberRunner.RegisterScenarios(new PaymentsControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/payments-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class DeliveryLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public DeliveryLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void DeliveryController_LoadTest() { NBomberRunner.RegisterScenarios(new DeliveryControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/delivery-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ReviewsLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ReviewsLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ReviewsController_LoadTest() { NBomberRunner.RegisterScenarios(new ReviewsControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/reviews-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class WishlistLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public WishlistLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void WishlistController_LoadTest() { NBomberRunner.RegisterScenarios(new WishlistControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/wishlist-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class UserLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public UserLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void UserController_LoadTest() { NBomberRunner.RegisterScenarios(new UserControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/user-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AddressesLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AddressesLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AddressesController_LoadTest() { NBomberRunner.RegisterScenarios(new AddressesControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/addresses-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class LoyaltyLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public LoyaltyLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void LoyaltyController_LoadTest() { NBomberRunner.RegisterScenarios(new LoyaltyControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/loyalty-load").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AdminDashboardLoadTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AdminDashboardLoadTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AdminDashboardController_LoadTest() { NBomberRunner.RegisterScenarios(new AdminDashboardControllerScenarios(_fixture).BuildLoadScenario()).WithReportFolder("reports/admin-dashboard-load").WithReportFileName("index.html").Run(); }
}
