using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuartzKnowledgeMcp.Api.Persistence;

namespace QuartzKnowledgeMcp.Tests.Security;

public class ApiKeyAuthenticationTests : IClassFixture<ApiKeyAuthenticationTests.AuthEnabledApiFactory>
{
    private readonly AuthEnabledApiFactory _factory;

    public ApiKeyAuthenticationTests(AuthEnabledApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedApi_RejectsMissingApiKey_WhenEnabled()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedApi_AllowsConfiguredApiKey_WhenEnabled()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthEnabledApiFactory.HeaderName, AuthEnabledApiFactory.ApiKey);

        var response = await client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_RemainsAnonymous_WhenAuthEnabled()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public sealed class AuthEnabledApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string HeaderName = "X-QuartzKnowledge-Api-Key";
        public const string ApiKey = "test-quartz-knowledge-key";

        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"quartz-knowledge-auth-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:KnowledgeStore"] = $"Data Source={_databasePath}",
                    ["Authentication:ApiKey:Enabled"] = "true",
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:ApiKey"] = ApiKey,
                    ["Authentication:ApiKey:ProtectedPrefixes:0"] = "/api",
                    ["Authentication:ApiKey:ProtectedPrefixes:1"] = "/mcp"
                });
            });
        }

        public async Task InitializeAsync()
        {
            await ResetDatabaseAsync();
        }

        public new async Task DisposeAsync()
        {
            await base.DisposeAsync();

            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
            }
        }

        public async Task ResetDatabaseAsync()
        {
            await using var scope = Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<McpKnowledgeDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();
        }
    }
}