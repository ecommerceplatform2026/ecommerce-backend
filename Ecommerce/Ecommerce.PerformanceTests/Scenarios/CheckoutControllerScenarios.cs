using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class CheckoutControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public CheckoutControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("checkout_load", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse addToCartResult = await Step.Run("add_to_cart", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new { ProductVariantId = _fixture.VariantId, Quantity = 1 },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            if (addToCartResult.IsError)
                return addToCartResult;

            IResponse checkoutResult = await Step.Run("checkout", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/checkout",
                    new { PaymentMethod = "COD" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return checkoutResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(5, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(5, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("checkout_stress", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse addToCartResult = await Step.Run("add_to_cart", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new { ProductVariantId = _fixture.VariantId, Quantity = 1 },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            if (addToCartResult.IsError)
                return addToCartResult;

            IResponse checkoutResult = await Step.Run("checkout", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/checkout",
                    new { PaymentMethod = "COD" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return checkoutResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(2, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(5, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(20, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        )
        .WithWarmUpDuration(TimeSpan.FromSeconds(3));
    }

    public ScenarioProps BuildEnduranceScenario()
    {
        return Scenario.Create("checkout_endurance", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse addToCartResult = await Step.Run("add_to_cart", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new { ProductVariantId = _fixture.VariantId, Quantity = 1 },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            if (addToCartResult.IsError)
                return addToCartResult;

            IResponse checkoutResult = await Step.Run("checkout", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/checkout",
                    new { PaymentMethod = "COD" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return checkoutResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(5, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
