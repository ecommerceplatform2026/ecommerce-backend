using Ecommerce.PerformanceTests.Scenarios;
using NBomber.CSharp;

namespace Ecommerce.PerformanceTests;

[Collection("PerformanceTests")]
public sealed class AuthEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AuthEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AuthController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AuthControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/auth-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductsEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ProductsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ProductsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ProductsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/products-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CategoriesEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CategoriesEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CategoriesController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CategoriesControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/categories-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductVariantsEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ProductVariantsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ProductVariantsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ProductVariantsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/product-variants-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CartEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CartEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CartController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CartControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/cart-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CheckoutEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public CheckoutEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void CheckoutController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CheckoutControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/checkout-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class OrdersEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public OrdersEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void OrdersController_EnduranceTest() { NBomberRunner.RegisterScenarios(new OrdersControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/orders-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class PaymentsEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public PaymentsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void PaymentsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new PaymentsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/payments-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class DeliveryEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public DeliveryEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void DeliveryController_EnduranceTest() { NBomberRunner.RegisterScenarios(new DeliveryControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/delivery-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ReviewsEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public ReviewsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void ReviewsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ReviewsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/reviews-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class WishlistEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public WishlistEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void WishlistController_EnduranceTest() { NBomberRunner.RegisterScenarios(new WishlistControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/wishlist-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class UserEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public UserEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void UserController_EnduranceTest() { NBomberRunner.RegisterScenarios(new UserControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/user-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AddressesEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AddressesEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AddressesController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AddressesControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/addresses-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class LoyaltyEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public LoyaltyEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void LoyaltyController_EnduranceTest() { NBomberRunner.RegisterScenarios(new LoyaltyControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/loyalty-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AdminDashboardEnduranceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    public AdminDashboardEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [Fact]
    public void AdminDashboardController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AdminDashboardControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/admin-dashboard-endurance").WithReportFileName("index.html").Run(); }
}
