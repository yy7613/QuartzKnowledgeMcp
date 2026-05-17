using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using QuartzKnowledgeMcp.Api.Health;

namespace QuartzKnowledgeMcp.Tests.Health;

public class HealthStatusServiceTests
{
    [Fact]
    public void GetStatus_ReturnsHealthyStatus()
    {
        var now = new DateTimeOffset(2026, 5, 17, 12, 30, 0, TimeSpan.Zero);
        var service = new HealthStatusService(
            new HealthCheckOptions("ok", "Test.Api"),
            new TestHostEnvironment("Test"),
            new FixedTimeProvider(now));

        var status = service.GetStatus();

        Assert.Equal("ok", status.Status);
        Assert.Equal("Test.Api", status.ComponentName);
        Assert.Equal("Test", status.Environment);
        Assert.Equal(now, status.CheckedAtUtc);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "QuartzKnowledgeMcp.Tests";

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
