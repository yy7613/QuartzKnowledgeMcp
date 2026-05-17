using Microsoft.Extensions.Configuration;
using QuartzKnowledgeMcp.Api.Health;

namespace QuartzKnowledgeMcp.Tests.Health;

public class HealthCheckOptionsTests
{
    [Fact]
    public void FromConfiguration_UsesDefaults_WhenHealthSectionIsMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var options = HealthCheckOptions.FromConfiguration(configuration);

        Assert.Equal(HealthCheckOptions.DefaultStatus, options.Status);
        Assert.Equal(HealthCheckOptions.DefaultComponentName, options.ComponentName);
    }

    [Fact]
    public void FromConfiguration_TrimsConfiguredValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Health:Status"] = " ready ",
                ["Health:ComponentName"] = " Test.Api "
            })
            .Build();

        var options = HealthCheckOptions.FromConfiguration(configuration);

        Assert.Equal("ready", options.Status);
        Assert.Equal("Test.Api", options.ComponentName);
    }
}
