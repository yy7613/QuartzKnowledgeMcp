using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace QuartzKnowledgeMcp.Api.Security;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IOptionsMonitor<ApiKeyAuthenticationOptions> optionsMonitor)
    {
        var options = optionsMonitor.Get(ApiKeyAuthenticationDefaults.Scheme);
        if (!options.Enabled || !RequiresAuthentication(context.Request.Path.Value, options.ProtectedPrefixes))
        {
            await next(context);
            return;
        }

        var result = await context.AuthenticateAsync(ApiKeyAuthenticationDefaults.Scheme);
        if (!result.Succeeded || result.Principal is null)
        {
            await context.ChallengeAsync(ApiKeyAuthenticationDefaults.Scheme);
            return;
        }

        context.User = result.Principal;
        await next(context);
    }

    private static bool RequiresAuthentication(string? path, IEnumerable<string> protectedPrefixes)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        foreach (var prefix in protectedPrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (path.Length == prefix.Length || path[prefix.Length] == '/' || prefix.EndsWith('/')))
            {
                return true;
            }
        }

        return false;
    }
}