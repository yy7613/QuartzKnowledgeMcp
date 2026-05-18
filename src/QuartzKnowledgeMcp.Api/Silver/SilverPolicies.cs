namespace QuartzKnowledgeMcp.Api.Silver;

public static class SilverOrganizeModes
{
    public const string SilverDraft = "silver-draft";

    public static string Normalize(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode)
            ? SilverDraft
            : mode.Trim().ToLowerInvariant();
    }

    public static bool IsSupported(string? mode)
    {
        return string.Equals(
            Normalize(mode),
            SilverDraft,
            StringComparison.Ordinal);
    }
}