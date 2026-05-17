using QuartzKnowledgeMcp.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(HealthCheckOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IHealthStatusService, HealthStatusService>();

var app = builder.Build();

app.MapGet("/health", (IHealthStatusService healthStatusService) =>
    Results.Ok(healthStatusService.GetStatus()))
    .WithName("GetHealth");

app.Run();

public partial class Program
{
}
