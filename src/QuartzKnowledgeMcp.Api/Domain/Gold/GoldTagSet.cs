using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Domain.Gold;

public sealed class GoldTagSet
{
    private GoldTagSet(IReadOnlyList<string> values)
    {
        Values = values;
    }

    public IReadOnlyList<string> Values { get; }

    public static GoldTagSet Create(IReadOnlyList<string>? tags)
    {
        if (tags is null)
        {
            throw new GoldValidationException(new Dictionary<string, string[]>
            {
                ["tags"] =
                [
                    "tags is required."
                ]
            });
        }

        var errors = new Dictionary<string, string[]>();

        if (tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
        {
            errors["tags"] =
            [
                "tags must not contain empty values."
            ];
        }

        var normalized = tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .ToList();

        if (normalized.Count < 1 || normalized.Count > 5)
        {
            errors["tags"] =
            [
                "tags must contain between 1 and 5 values."
            ];
        }
        else if (normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Count)
        {
            errors["tags"] =
            [
                "tags must not contain duplicates."
            ];
        }

        if (errors.Count > 0)
        {
            throw new GoldValidationException(errors);
        }

        return new GoldTagSet(normalized);
    }
}