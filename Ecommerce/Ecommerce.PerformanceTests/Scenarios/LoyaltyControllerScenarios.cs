using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class LoyaltyControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public LoyaltyControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("loyalty_load", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse balanceResult = await Step.Run("balance", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/loyalty/balance",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse transactionsResult = await Step.Run("transactions", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/loyalty/transactions?Page=1&PageSize=10",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return transactionsResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("loyalty_stress", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse balanceResult = await Step.Run("balance", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/loyalty/balance",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return balanceResult;
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
        return Scenario.Create("loyalty_endurance", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse balanceResult = await Step.Run("balance", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/loyalty/balance",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return balanceResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
