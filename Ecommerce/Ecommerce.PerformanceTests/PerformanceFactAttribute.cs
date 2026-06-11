namespace Ecommerce.PerformanceTests;

public sealed class PerformanceFactAttribute : FactAttribute
{
    public PerformanceFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_PERF_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set RUN_PERF_TESTS=true to run performance tests";
        }
    }
}
