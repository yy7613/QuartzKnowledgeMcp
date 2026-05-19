using Microsoft.AspNetCore.Authentication;

namespace QuartzKnowledgeMcp.Api.Security;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string SectionName = "Authentication:ApiKey";

    public bool Enabled { get; set; }

    public string HeaderName { get; set; } = ApiKeyAuthenticationDefaults.HeaderName;

    public string? ApiKey { get; set; }

    public string[] ProtectedPrefixes { get; set; } = ["/api", "/mcp"];
}