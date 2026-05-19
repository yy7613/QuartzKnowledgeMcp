using System.Text;
using System.Text.Json.Serialization;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed record FoundrySilverToolDraftResponse(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description);

public sealed record FoundrySilverDraftResponse(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("summary")] string? Summary,
    [property: JsonPropertyName("tagCandidates")] IReadOnlyList<string>? TagCandidates,
    [property: JsonPropertyName("toolDrafts")] IReadOnlyList<FoundrySilverToolDraftResponse>? ToolDrafts)
{
    public SilverServerDraftContent ToSilverDraftContent()
    {
        var name = NormalizeRequired(Name, "name", 100);
        var summary = NormalizeRequired(Summary, "summary", 240);
        var tags = (TagCandidates ?? [])
            .Select(tag => CollapseWhitespace(tag))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        var toolDrafts = (ToolDrafts ?? [])
            .Select(ToToolDraftContent)
            .Where(tool => tool is not null)
            .Cast<SilverToolDraftContent>()
            .DistinctBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SilverServerDraftContent(name, summary, tags, toolDrafts);
    }

    private static SilverToolDraftContent? ToToolDraftContent(FoundrySilverToolDraftResponse? toolDraft)
    {
        if (toolDraft is null)
        {
            return null;
        }

        var name = CollapseWhitespace(toolDraft.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var description = CollapseWhitespace(toolDraft.Description);
        if (string.IsNullOrWhiteSpace(description))
        {
            description = "Description pending AI organization.";
        }

        return new SilverToolDraftContent(
            Truncate(name, 100),
            Truncate(description, 240));
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        var normalized = CollapseWhitespace(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new SilverNormalizationException($"Foundry response field '{fieldName}' was empty.");
        }

        return Truncate(normalized, maxLength);
    }

    private static string CollapseWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            previousWasWhitespace = false;
            builder.Append(character);
        }

        return builder.ToString();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}