using System.Globalization;
using System.Text.RegularExpressions;
using QuartzKnowledgeMcp.Api.Bronze;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed partial class RuleBasedSilverNormalizer
{
    private static readonly string[] ToolSectionTitles =
    [
        "tools",
        "available tools",
        "capabilities",
        "commands"
    ];

    private static readonly (string Pattern, string Tag)[] TagPatterns =
    [
        ("mcp", "mcp"),
        ("github", "github"),
        ("search", "search"),
        ("rag", "rag"),
        ("slack", "slack"),
        ("notion", "notion"),
        ("database", "database"),
        ("sqlite", "sqlite"),
        ("postgres", "postgres"),
        ("azure", "azure"),
        ("aws", "aws"),
        ("cli", "cli")
    ];

    public SilverServerDraftContent Normalize(BronzeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var lines = NormalizeLines(source.RawContent);
        var name = ExtractName(lines)
            ?? ExtractNameFromUri(source.SourceUri)
            ?? "Untitled MCP Server";
        var summary = ExtractSummary(lines, name);
        var tags = ExtractTags(lines, source);
        var toolDrafts = ExtractToolDrafts(lines);

        return new SilverServerDraftContent(name, summary, tags, toolDrafts);
    }

    private static IReadOnlyList<string> NormalizeLines(string rawContent)
    {
        var lines = new List<string>();
        var inCodeFence = false;

        foreach (var rawLine in rawContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var trimmedLine = rawLine.TrimEnd();
            var normalized = trimmedLine.TrimStart();

            if (normalized.StartsWith("```", StringComparison.Ordinal))
            {
                inCodeFence = !inCodeFence;
                continue;
            }

            if (inCodeFence)
            {
                continue;
            }

            lines.Add(trimmedLine);
        }

        return lines;
    }

    private static string? ExtractName(IReadOnlyList<string> lines)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || IsDecorative(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                return Truncate(CleanMarkdown(trimmed.TrimStart('#', ' ')), 100);
            }

            if (IsLikelyStandaloneTitle(trimmed))
            {
                return Truncate(CleanMarkdown(trimmed), 100);
            }

            break;
        }

        return null;
    }

    private static string ExtractSummary(IReadOnlyList<string> lines, string name)
    {
        var paragraphLines = new List<string>();
        var titleSkipped = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (IsDecorative(trimmed))
            {
                continue;
            }

            if (!titleSkipped && (trimmed.StartsWith("#", StringComparison.Ordinal) || IsLikelyStandaloneTitle(trimmed)))
            {
                titleSkipped = true;
                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (IsListLike(trimmed) || trimmed.StartsWith("|", StringComparison.Ordinal) || trimmed.StartsWith(">", StringComparison.Ordinal))
            {
                if (paragraphLines.Count > 0)
                {
                    break;
                }

                continue;
            }

            var candidate = CleanMarkdown(trimmed);
            if (string.IsNullOrWhiteSpace(candidate) || string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            paragraphLines.Add(candidate);

            if (string.Join(" ", paragraphLines).Length >= 240)
            {
                break;
            }
        }

        var summary = CollapseWhitespace(string.Join(" ", paragraphLines));
        return string.IsNullOrWhiteSpace(summary)
            ? "Summary pending normalization."
            : Truncate(summary, 240);
    }

    private static IReadOnlyList<string> ExtractTags(IReadOnlyList<string> lines, BronzeSource source)
    {
        var content = string.Join("\n", lines).ToLowerInvariant();
        var tags = new List<string>();

        AddTag(tags, "mcp");

        if (string.Equals(source.SourceType, "github-readme", StringComparison.OrdinalIgnoreCase))
        {
            AddTag(tags, "github");
        }

        foreach (var (pattern, tag) in TagPatterns)
        {
            if (content.Contains(pattern, StringComparison.Ordinal))
            {
                AddTag(tags, tag);
            }
        }

        return tags;
    }

    private static IReadOnlyList<SilverToolDraftContent> ExtractToolDrafts(IReadOnlyList<string> lines)
    {
        var toolDrafts = new List<SilverToolDraftContent>();
        var inToolsSection = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (inToolsSection && toolDrafts.Count > 0)
                {
                    break;
                }

                continue;
            }

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                var heading = CleanMarkdown(trimmed.TrimStart('#', ' ')).ToLowerInvariant();
                if (ToolSectionTitles.Contains(heading, StringComparer.OrdinalIgnoreCase))
                {
                    inToolsSection = true;
                    continue;
                }

                if (inToolsSection)
                {
                    break;
                }

                continue;
            }

            if (!inToolsSection)
            {
                continue;
            }

            if (!TryParseTool(trimmed, out var toolDraft))
            {
                if (toolDrafts.Count > 0)
                {
                    break;
                }

                continue;
            }

            toolDrafts.Add(toolDraft);

            if (toolDrafts.Count == 10)
            {
                break;
            }
        }

        return toolDrafts;
    }

    private static bool TryParseTool(string line, out SilverToolDraftContent toolDraft)
    {
        var withoutBullet = BulletPrefixRegex().Replace(line, string.Empty);
        var separatorIndex = withoutBullet.IndexOf(':');
        if (separatorIndex < 0)
        {
            separatorIndex = withoutBullet.IndexOf(" - ", StringComparison.Ordinal);
        }

        if (separatorIndex <= 0)
        {
            toolDraft = default!;
            return false;
        }

        var name = CleanMarkdown(withoutBullet[..separatorIndex].Trim());
        var descriptionStart = withoutBullet[separatorIndex] == ':'
            ? separatorIndex + 1
            : separatorIndex + 3;
        var description = CleanMarkdown(withoutBullet[descriptionStart..].Trim());

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            toolDraft = default!;
            return false;
        }

        toolDraft = new SilverToolDraftContent(name, Truncate(description, 240));
        return true;
    }

    private static string? ExtractNameFromUri(string? sourceUri)
    {
        if (string.IsNullOrWhiteSpace(sourceUri) || !Uri.TryCreate(sourceUri, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var lastSegment = uri.Segments.LastOrDefault()?.Trim('/');
        if (string.IsNullOrWhiteSpace(lastSegment))
        {
            return null;
        }

        return HumanizeIdentifier(lastSegment);
    }

    private static string HumanizeIdentifier(string value)
    {
        var tokens = value
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace('.', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return "Untitled MCP Server";
        }

        return string.Join(
            " ",
            tokens.Select(token => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(token.ToLowerInvariant())));
    }

    private static bool IsDecorative(string line)
    {
        return line.StartsWith("![", StringComparison.Ordinal)
            || line.StartsWith("[![", StringComparison.Ordinal)
            || line.StartsWith("<!--", StringComparison.Ordinal);
    }

    private static bool IsLikelyStandaloneTitle(string line)
    {
        return !IsListLike(line)
            && !line.Contains('.', StringComparison.Ordinal)
            && !line.Contains(':', StringComparison.Ordinal)
            && line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 8
            && line.Length <= 80;
    }

    private static bool IsListLike(string line)
    {
        return BulletPrefixRegex().IsMatch(line);
    }

    private static void AddTag(ICollection<string> tags, string tag)
    {
        if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase) && tags.Count < 6)
        {
            tags.Add(tag);
        }
    }

    private static string CleanMarkdown(string value)
    {
        var cleaned = MarkdownLinkRegex().Replace(value, "$1");
        cleaned = cleaned.Replace("`", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("**", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("__", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("*", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("_", " ", StringComparison.Ordinal);
        return CollapseWhitespace(cleaned.Trim());
    }

    private static string CollapseWhitespace(string value)
    {
        return MultiWhitespaceRegex().Replace(value, " ").Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : string.Concat(value[..(maxLength - 3)].TrimEnd(), "...");
    }

    [GeneratedRegex(@"^(?:[-*+]\s+|\d+\.\s+)", RegexOptions.Compiled)]
    private static partial Regex BulletPrefixRegex();

    [GeneratedRegex(@"\[(.*?)\]\([^\)]*\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultiWhitespaceRegex();
}