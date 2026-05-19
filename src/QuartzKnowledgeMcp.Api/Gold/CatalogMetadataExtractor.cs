namespace QuartzKnowledgeMcp.Api.Gold;

public static class CatalogMetadataExtractor
{
    private static readonly (string Label, string[] Patterns)[] SupportedClients =
    [
        ("VS Code", ["vs code", "vscode"]),
        ("Claude Desktop", ["claude desktop"]),
        ("Cursor", ["cursor"]),
        ("Cline", ["cline"]),
        ("Windsurf", ["windsurf"]),
        ("ChatGPT Desktop", ["chatgpt desktop"])
    ];

    public static string DetectAuthenticationType(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return "unknown";
        }

        var content = rawContent.ToLowerInvariant();

        if (ContainsAny(content, "oauth", "openid", "oidc"))
        {
            return "oauth";
        }

        if (ContainsAny(content, "api key", "api-key", "apikey", "bearer token", "personal access token"))
        {
            return "api-key";
        }

        if (ContainsAny(
            content,
            "no authentication required",
            "without authentication",
            "no auth",
            "anonymous access",
            "authentication: none",
            "authentication none",
            "auth: none"))
        {
            return "none";
        }

        return "unknown";
    }

    public static IReadOnlyList<string> DetectSupportedClients(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return [];
        }

        var content = rawContent.ToLowerInvariant();
        var detected = new List<string>();

        foreach (var (label, patterns) in SupportedClients)
        {
            if (patterns.Any(pattern => content.Contains(pattern, StringComparison.Ordinal)))
            {
                detected.Add(label);
            }
        }

        return detected;
    }

    private static bool ContainsAny(string content, params string[] values)
    {
        return values.Any(value => content.Contains(value, StringComparison.Ordinal));
    }
}