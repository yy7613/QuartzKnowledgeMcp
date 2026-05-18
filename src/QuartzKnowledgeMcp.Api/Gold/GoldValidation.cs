namespace QuartzKnowledgeMcp.Api.Gold;

public static class GoldValidation
{
    public static void ValidateCatalogUpdate(UpdateGoldCatalogEntryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Overview))
        {
            errors["overview"] =
            [
                "overview is required."
            ];
        }

        if (string.IsNullOrWhiteSpace(request.SetupGuide))
        {
            errors["setupGuide"] =
            [
                "setupGuide is required."
            ];
        }

        if (request.References is not null
            && request.References.Any(reference => string.IsNullOrWhiteSpace(reference)))
        {
            errors["references"] =
            [
                "references must not contain empty values."
            ];
        }

        if (request.SupportedClients is not null)
        {
            if (request.SupportedClients.Any(client => string.IsNullOrWhiteSpace(client)))
            {
                errors["supportedClients"] =
                [
                    "supportedClients must not contain empty values."
                ];
            }
            else if (request.SupportedClients
                .Select(client => client.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != request.SupportedClients.Count)
            {
                errors["supportedClients"] =
                [
                    "supportedClients must not contain duplicates."
                ];
            }
        }

        if (errors.Count > 0)
        {
            throw new GoldValidationException(errors);
        }
    }

    public static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
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

        return normalized;
    }

    public static IReadOnlyList<string> NormalizeReferences(IReadOnlyList<string>? references)
    {
        return references is null
            ? []
            : references.Select(reference => reference.Trim()).ToList();
    }

    public static IReadOnlyList<string> NormalizeSupportedClients(IReadOnlyList<string>? supportedClients)
    {
        return supportedClients is null
            ? []
            : supportedClients.Select(client => client.Trim()).ToList();
    }
}