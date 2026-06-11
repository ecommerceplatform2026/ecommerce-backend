using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class AddressesControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public AddressesControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("addresses_load", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/addresses",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse getByIdResult = await Step.Run("get_by_id", context, async () =>
            {
                var response = await client.GetAsync(
                    $"/api/addresses/{_fixture.AddressId}",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse createResult = await Step.Run("create", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/addresses",
                    new { ReceiverName = "Perf Address", PhoneNumber = "0900000001", AddressLine = "456 Test Ave", IsDefault = false },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return createResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("addresses_stress", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/addresses",
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
        return Scenario.Create("addresses_endurance", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.UserToken);

            IResponse listResult = await Step.Run("list", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/addresses",
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
