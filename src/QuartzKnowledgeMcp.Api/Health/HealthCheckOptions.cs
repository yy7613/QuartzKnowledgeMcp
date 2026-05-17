using Microsoft.Extensions.Configuration;

namespace QuartzKnowledgeMcp.Api.Health;

public sealed record HealthCheckOptions(string Status, string ComponentName)
{
    public const string SectionName = "Health";
    public const string DefaultStatus = "ok";
    public const string DefaultComponentName = "QuartzKnowledgeMcp.Api";

    public static HealthCheckOptions Default { get; } = new(
        DefaultStatus,
        DefaultComponentName);

    public static HealthCheckOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var status = ResolveValue(section["Status"], DefaultStatus);
        var componentName = ResolveValue(section["ComponentName"], DefaultComponentName);

        return new HealthCheckOptions(status, componentName);
    }

    private static string ResolveValue(string? configuredValue, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(configuredValue)
            ? defaultValue
            : configuredValue.Trim();
    }
}
