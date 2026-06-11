using NBomber.CSharp;
using NBomber.Contracts;

namespace Ecommerce.PerformanceTests.Scenarios;

public sealed class AdminDashboardControllerScenarios
{
    private readonly PerformanceTestFixture _fixture;

    public AdminDashboardControllerScenarios(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
    }

    public ScenarioProps BuildLoadScenario()
    {
        return Scenario.Create("admin_dashboard_load", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.AdminToken);

            IResponse summaryResult = await Step.Run("summary", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/admin/dashboard/summary",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse revenueResult = await Step.Run("revenue_trend", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/admin/dashboard/revenue-trend",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            IResponse paymentMethodsResult = await Step.Run("payment_methods", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/admin/dashboard/payment-methods",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return paymentMethodsResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(10, TimeSpan.FromSeconds(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(5))
        );
    }

    public ScenarioProps BuildStressScenario()
    {
        return Scenario.Create("admin_dashboard_stress", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.AdminToken);

            IResponse summaryResult = await Step.Run("summary", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/admin/dashboard/summary",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return summaryResult;
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
        return Scenario.Create("admin_dashboard_endurance", async context =>
        {
            var client = _fixture.CreateAuthenticatedClient(_fixture.AdminToken);

            IResponse summaryResult = await Step.Run("summary", context, async () =>
            {
                var response = await client.GetAsync(
                    "/api/admin/dashboard/summary",
                    context.ScenarioCancellationToken);
                return response.IsSuccessStatusCode
                    ? Response.Ok(statusCode: ((int)response.StatusCode).ToString())
                    : Response.Fail(statusCode: ((int)response.StatusCode).ToString());
            });

            return summaryResult;
        })
        .WithLoadSimulations(
            Simulation.RampingConstant(10, TimeSpan.FromSeconds(10)),
            Simulation.KeepConstant(30, TimeSpan.FromMinutes(30)),
            Simulation.RampingConstant(0, TimeSpan.FromSeconds(10))
        );
    }
}
