using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class CartControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public CartControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("cart_load", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse addItemResult = await Step.Run("add_item", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new { ProductVariantId = _fixture.VariantId, Quantity = 1 },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse getCartResult = await Step.Run("get_cart", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/cart",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return getCartResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("cart_stress", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse addItemResult = await Step.Run("add_item", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/cart/items",
                    new { ProductVariantId = _fixture.VariantId, Quantity = 1 },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return addItemResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(5, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(20, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(50, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(100, TimeSpan.FromSeconds(10)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        )
        .WithWarmUpDuration(TimeSpan.FromSeconds(3));
    }

    public ScenarioProps BuildEnduranceScenario()
    {
        return Scenario.Create("cart_endurance", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse getCartResult = await Step.Run("get_cart", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/cart",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return getCartResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
