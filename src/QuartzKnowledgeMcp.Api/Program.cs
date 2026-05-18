using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using QuartzKnowledgeMcp.Api.Application;
using QuartzKnowledgeMcp.Api.Capabilities;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Domain.Ports;
using QuartzKnowledgeMcp.Api.Gold;
using QuartzKnowledgeMcp.Api.Health;
using QuartzKnowledgeMcp.Api.Mcp;
using QuartzKnowledgeMcp.Api.Persistence;
using QuartzKnowledgeMcp.Api.Search;
using QuartzKnowledgeMcp.Api.Silver;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton(HealthCheckOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IHealthStatusService, HealthStatusService>();
builder.Services.AddDbContext<McpKnowledgeDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("KnowledgeStore")
        ?? "Data Source=quartz-knowledge.db";

    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<BronzeIngestionService>();
builder.Services.AddScoped<IKnowledgeRepository, SqliteKnowledgeRepository>();
builder.Services.AddScoped<IHistoryRepository, SqliteHistoryRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<RuleBasedSilverNormalizer>();
builder.Services.AddScoped<IOrganizationAgent, RuleBasedOrganizationAgent>();
builder.Services.AddScoped<SilverDraftService>();
builder.Services.AddScoped<GoldCatalogService>();
builder.Services.AddScoped<SilverDraftApplicationService>();
builder.Services.AddScoped<CatalogCurationApplicationService>();
builder.Services.AddScoped<CatalogSearchService>();
builder.Services.AddScoped<SystemCapabilitiesService>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithTools<HealthMcpTools>()
    .WithTools<BronzeMcpTools>()
    .WithTools<CatalogMcpTools>()
    .WithTools<SearchMcpTools>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<McpKnowledgeDbContext>();
    dbContext.Database.Migrate();
}

app.MapGet("/health", (IHealthStatusService healthStatusService) =>
    Results.Ok(healthStatusService.GetStatus()))
    .WithName("GetHealth");

app.MapBronzeEndpoints();
app.MapSilverEndpoints();
app.MapGoldEndpoints();
app.MapSearchEndpoints();
app.MapSystemEndpoints();
app.MapMcp("/mcp");

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program
{
}
