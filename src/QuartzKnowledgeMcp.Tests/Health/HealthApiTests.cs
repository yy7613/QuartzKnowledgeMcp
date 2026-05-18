using System.Net;
using System.Net.Http.Json;
using QuartzKnowledgeMcp.Tests.Infrastructure;

namespace QuartzKnowledgeMcp.Tests.Health;

[Collection(ApiTestCollection.Name)]
public class HealthApiTests(ApiTestFactory factory)
{
    [Fact]
    public async Task GetHealth_ReturnsOkStatus()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        var payload = await response.Content.ReadFromJsonAsync<HealthPayload>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("ok", payload.Status);
        Assert.Equal("QuartzKnowledgeMcp.Api", payload.ComponentName);
    }

    private sealed record HealthPayload(
        string Status,
        string ComponentName,
        DateTime CheckedAtUtc);
}