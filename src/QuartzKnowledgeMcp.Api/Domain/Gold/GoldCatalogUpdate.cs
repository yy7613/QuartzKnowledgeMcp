using QuartzKnowledgeMcp.Api.Gold;

namespace QuartzKnowledgeMcp.Api.Domain.Gold;

public sealed class GoldCatalogUpdate
{
    private GoldCatalogUpdate(
        string overview,
        string setupGuide,
        IReadOnlyList<string> references,
        IReadOnlyList<string> supportedClients)
    {
        Overview = overview;
        SetupGuide = setupGuide;
        References = references;
        SupportedClients = supportedClients;
    }

    public string Overview { get; }

    public string SetupGuide { get; }

    public IReadOnlyList<string> References { get; }

    public IReadOnlyList<string> SupportedClients { get; }

    public static GoldCatalogUpdate Create(
        string? overview,
        string? setupGuide,
        IReadOnlyList<string>? references,
        IReadOnlyList<string>? supportedClients)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(overview))
        {
            errors["overview"] =
            [
                "overview is required."
            ];
        }

        if (string.IsNullOrWhiteSpace(setupGuide))
        {
            errors["setupGuide"] =
            [
                "setupGuide is required."
            ];
        }

        if (references is not null
            && references.Any(reference => string.IsNullOrWhiteSpace(reference)))
        {
            errors["references"] =
            [
                "references must not contain empty values."
            ];
        }

        List<string> normalizedSupportedClients = [];
        if (supportedClients is not null)
        {
            if (supportedClients.Any(client => string.IsNullOrWhiteSpace(client)))
            {
                errors["supportedClients"] =
                [
                    "supportedClients must not contain empty values."
                ];
            }
            else
            {
                normalizedSupportedClients = supportedClients
                    .Select(client => client.Trim())
                    .ToList();

                if (normalizedSupportedClients
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() != normalizedSupportedClients.Count)
                {
                    errors["supportedClients"] =
                    [
                        "supportedClients must not contain duplicates."
                    ];
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new GoldValidationException(errors);
        }

        return new GoldCatalogUpdate(
            overview!.Trim(),
            setupGuide!.Trim(),
            references is null
                ? []
                : references.Select(reference => reference.Trim()).ToList(),
            normalizedSupportedClients);
    }
}