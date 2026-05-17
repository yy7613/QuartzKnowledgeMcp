namespace QuartzKnowledgeMcp.Api.Bronze;

public static class BronzeSourceStatuses
{
    public const string Imported = "imported";
}

public static class BronzeSourceTypes
{
    private static readonly HashSet<string> AllowedValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "manual",
        "github-readme",
        "docs-url",
        "json-import"
    };

    public static bool IsAllowed(string? sourceType)
    {
        return !string.IsNullOrWhiteSpace(sourceType)
            && AllowedValues.Contains(sourceType.Trim());
    }

    public static string Normalize(string sourceType)
    {
        return sourceType.Trim().ToLowerInvariant();
    }
}
