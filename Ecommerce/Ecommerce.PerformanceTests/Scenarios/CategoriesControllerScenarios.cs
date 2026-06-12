using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class CategoriesControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public CategoriesControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("categories_load", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/categories",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return listResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("categories_stress", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/categories",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return listResult;
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
        return Scenario.Create("categories_endurance", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/categories",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return listResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
