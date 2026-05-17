using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using QuartzKnowledgeMcp.Api.Bronze;
using QuartzKnowledgeMcp.Api.Health;
using QuartzKnowledgeMcp.Api.Persistence;

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

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program
{
}
