namespace QuartzKnowledgeMcp.Api.Health;

public sealed record HealthStatus(
    string Status,
    string ComponentName,
    string Environment,
    DateTimeOffset CheckedAtUtc);
