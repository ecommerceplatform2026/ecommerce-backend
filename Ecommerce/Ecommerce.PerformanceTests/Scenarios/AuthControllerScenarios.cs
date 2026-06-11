using System.Net.Http.Json;
using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class AuthControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public AuthControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("auth_load", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse loginResult = await Step.Run("login", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { Email = "user@test.com", Password = "User123!" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse registerResult = await Step.Run("register", context, async () =>
            {
                var email = $"perfuser_{Guid.NewGuid():N}@test.com";
                var response = await client.PostAsJsonAsync(
                    "/api/auth/register",
                    new { FullName = "Perf User", Email = email, Password = "PerfPass123!" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return registerResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("auth_stress", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse loginResult = await Step.Run("login", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { Email = "user@test.com", Password = "User123!" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return loginResult;
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
        return Scenario.Create("auth_endurance", async context =>
        {
            var client = _fixture.CreateClient();

            IResponse loginResult = await Step.Run("login", context, async () =>
            {
                var response = await client.PostAsJsonAsync(
                    "/api/auth/login",
                    new { Email = "user@test.com", Password = "User123!" },
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return loginResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
