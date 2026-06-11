using Ecommerce.PerformanceTests.Scenarios;
using NBomber.CSharp;

namespace Ecommerce.PerformanceTests;

[Collection("PerformanceTests")]
public sealed class AuthEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public AuthEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AuthController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AuthControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/auth-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductsEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public ProductsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ProductsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ProductsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/products-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CategoriesEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public CategoriesEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CategoriesController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CategoriesControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/categories-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductVariantsEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public ProductVariantsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ProductVariantsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ProductVariantsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/product-variants-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CartEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public CartEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CartController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CartControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/cart-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CheckoutEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public CheckoutEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CheckoutController_EnduranceTest() { NBomberRunner.RegisterScenarios(new CheckoutControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/checkout-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class OrdersEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public OrdersEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void OrdersController_EnduranceTest() { NBomberRunner.RegisterScenarios(new OrdersControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/orders-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class PaymentsEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public PaymentsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void PaymentsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new PaymentsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/payments-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class DeliveryEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public DeliveryEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void DeliveryController_EnduranceTest() { NBomberRunner.RegisterScenarios(new DeliveryControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/delivery-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ReviewsEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public ReviewsEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ReviewsController_EnduranceTest() { NBomberRunner.RegisterScenarios(new ReviewsControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/reviews-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class WishlistEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public WishlistEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void WishlistController_EnduranceTest() { NBomberRunner.RegisterScenarios(new WishlistControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/wishlist-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class UserEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public UserEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void UserController_EnduranceTest() { NBomberRunner.RegisterScenarios(new UserControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/user-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AddressesEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public AddressesEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AddressesController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AddressesControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/addresses-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class LoyaltyEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public LoyaltyEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void LoyaltyController_EnduranceTest() { NBomberRunner.RegisterScenarios(new LoyaltyControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/loyalty-endurance").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AdminDashboardEnduranceTests
{
    private readonly PerformanceTestFixture _fixture;
    public AdminDashboardEnduranceTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AdminDashboardController_EnduranceTest() { NBomberRunner.RegisterScenarios(new AdminDashboardControllerScenarios(_fixture).BuildEnduranceScenario()).WithReportFolder("reports/admin-dashboard-endurance").WithReportFileName("index.html").Run(); }
}
