using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace QuartzKnowledgeMcp.Api.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Enabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (string.IsNullOrWhiteSpace(Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is enabled but ApiKey is not configured."));
        }

        if (!Request.Headers.TryGetValue(Options.HeaderName, out StringValues values) || StringValues.IsNullOrEmpty(values))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing API key header '{Options.HeaderName}'."));
        }

        if (values.Count != 1)
        {
            return Task.FromResult(AuthenticateResult.Fail("Provide exactly one API key value."));
        }

        var providedValue = values[0] ?? string.Empty;
        if (!IsMatch(providedValue, Options.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ApiKeyAuthenticationDefaults.Scheme),
            new Claim(ClaimTypes.Name, "quartz-api-key-client")
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.Append("WWW-Authenticate", $"{ApiKeyAuthenticationDefaults.Scheme} realm=\"QuartzKnowledge\"");

        if (ShouldReturnJson(Request.Path.Value))
        {
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new
            {
                error = "unauthorized",
                message = $"Provide a valid API key using the '{Options.HeaderName}' header."
            });
        }
    }

    private static bool IsMatch(string providedValue, string expectedValue)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedValue);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedValue);

        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static bool ShouldReturnJson(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/mcp", StringComparison.OrdinalIgnoreCase);
    }
}