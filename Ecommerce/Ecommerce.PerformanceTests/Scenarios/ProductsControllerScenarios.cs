using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class ProductsControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public ProductsControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("products_load", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse searchResult = await Step.Run("search", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/search?SortBy=createdAt&SortDirection=desc&Page=1&PageSize=20",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse detailResult = await Step.Run("detail", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/{_fixture.ProductId}",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse detailFullResult = await Step.Run("detail_full", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/{_fixture.ProductId}/detail",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/products",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse imagesResult = await Step.Run("images", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/{_fixture.ProductId}/images",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return imagesResult;
        })
        .WithInit(async _ =>
        {
            var client = _fixture.CreateClient();
            await client.GetAsync($"/api/products/{_fixture.ProductId}");
            await client.GetAsync($"/api/products/{_fixture.ProductId}/detail");
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("products_stress", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse searchResult = await Step.Run("search", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/search?SortBy=createdAt&SortDirection=desc&Page=1&PageSize=20",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return searchResult;
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
        return Scenario.Create("products_endurance", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse searchResult = await Step.Run("search", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/products/search?SortBy=createdAt&SortDirection=desc&Page=1&PageSize=20",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return searchResult;
        })
        .WithInit(async _ =>
        {
            var client = _fixture.CreateClient();
            await client.GetAsync($"/api/products/{_fixture.ProductId}");
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
