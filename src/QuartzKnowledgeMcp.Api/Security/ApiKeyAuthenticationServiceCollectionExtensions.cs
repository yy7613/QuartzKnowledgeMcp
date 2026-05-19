using Microsoft.AspNetCore.Authentication;

namespace QuartzKnowledgeMcp.Api.Security;

public static class ApiKeyAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddQuartzKnowledgeAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiKeyAuthenticationOptions>(ApiKeyAuthenticationDefaults.Scheme)
            .Bind(configuration.GetSection(ApiKeyAuthenticationOptions.SectionName))
            .PostConfigure(NormalizeOptions);

        services.AddAuthentication(ApiKeyAuthenticationDefaults.Scheme)
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.Scheme,
                _ => { });

        services.AddAuthorization();
        return services;
    }

    public static IApplicationBuilder UseQuartzKnowledgeAuthentication(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseAuthorization();
        return app;
    }

    private static void NormalizeOptions(ApiKeyAuthenticationOptions options)
    {
        options.HeaderName = string.IsNullOrWhiteSpace(options.HeaderName)
            ? ApiKeyAuthenticationDefaults.HeaderName
            : options.HeaderName.Trim();

        options.ProtectedPrefixes = (options.ProtectedPrefixes ?? [])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (options.ProtectedPrefixes.Length == 0)
        {
            options.ProtectedPrefixes = ["/api", "/mcp"];
        }
    }
}