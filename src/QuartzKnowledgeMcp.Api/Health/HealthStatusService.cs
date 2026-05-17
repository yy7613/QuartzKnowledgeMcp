using Microsoft.Extensions.Hosting;

namespace QuartzKnowledgeMcp.Api.Health;

public interface IHealthStatusService
{
    HealthStatus GetStatus();
}

public sealed class HealthStatusService(
    HealthCheckOptions options,
    IHostEnvironment hostEnvironment,
    TimeProvider timeProvider) : IHealthStatusService
{
    public HealthStatus GetStatus()
    {
        return new HealthStatus(
            options.Status,
            options.ComponentName,
            hostEnvironment.EnvironmentName,
            timeProvider.GetUtcNow());
    }
}
