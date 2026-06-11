using Ecommerce.PerformanceTests.Scenarios;
using NBomber.CSharp;

namespace Ecommerce.PerformanceTests;

[Collection("PerformanceTests")]
public sealed class AuthStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public AuthStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AuthController_StressTest() { NBomberRunner.RegisterScenarios(new AuthControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/auth-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductsStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public ProductsStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ProductsController_StressTest() { NBomberRunner.RegisterScenarios(new ProductsControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/products-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CategoriesStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public CategoriesStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CategoriesController_StressTest() { NBomberRunner.RegisterScenarios(new CategoriesControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/categories-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ProductVariantsStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public ProductVariantsStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ProductVariantsController_StressTest() { NBomberRunner.RegisterScenarios(new ProductVariantsControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/product-variants-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CartStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public CartStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CartController_StressTest() { NBomberRunner.RegisterScenarios(new CartControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/cart-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class CheckoutStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public CheckoutStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void CheckoutController_StressTest() { NBomberRunner.RegisterScenarios(new CheckoutControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/checkout-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class OrdersStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public OrdersStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void OrdersController_StressTest() { NBomberRunner.RegisterScenarios(new OrdersControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/orders-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class PaymentsStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public PaymentsStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void PaymentsController_StressTest() { NBomberRunner.RegisterScenarios(new PaymentsControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/payments-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class DeliveryStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public DeliveryStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void DeliveryController_StressTest() { NBomberRunner.RegisterScenarios(new DeliveryControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/delivery-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class ReviewsStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public ReviewsStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void ReviewsController_StressTest() { NBomberRunner.RegisterScenarios(new ReviewsControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/reviews-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class WishlistStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public WishlistStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void WishlistController_StressTest() { NBomberRunner.RegisterScenarios(new WishlistControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/wishlist-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class UserStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public UserStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void UserController_StressTest() { NBomberRunner.RegisterScenarios(new UserControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/user-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AddressesStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public AddressesStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AddressesController_StressTest() { NBomberRunner.RegisterScenarios(new AddressesControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/addresses-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class LoyaltyStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public LoyaltyStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void LoyaltyController_StressTest() { NBomberRunner.RegisterScenarios(new LoyaltyControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/loyalty-stress").WithReportFileName("index.html").Run(); }
}

[Collection("PerformanceTests")]
public sealed class AdminDashboardStressTests
{
    private readonly PerformanceTestFixture _fixture;
    public AdminDashboardStressTests(PerformanceTestFixture fixture) { _fixture = fixture; }
    [PerformanceFact]
    public void AdminDashboardController_StressTest() { NBomberRunner.RegisterScenarios(new AdminDashboardControllerScenarios(_fixture).BuildStressScenario()).WithReportFolder("reports/admin-dashboard-stress").WithReportFileName("index.html").Run(); }
}
