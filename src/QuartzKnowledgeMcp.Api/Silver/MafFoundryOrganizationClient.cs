using Azure.AI.Projects;  
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class MafFoundryOrganizationClient(
    IOptions<FoundryOrganizationOptions> foundryOptions) : IFoundryOrganizationClient
{
    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var options = foundryOptions.Value;
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException("Foundry organization client requires enabled configuration with endpoint and model.");
        }

        var projectClient = new AIProjectClient(
            new Uri(options.ProjectEndpoint!, UriKind.Absolute),
            new DefaultAzureCredential());
        var responsesClient = projectClient.ProjectOpenAIClient
            .GetProjectResponsesClientForModel(options.EffectiveModel, defaultConversationId: null);
        var response = await responsesClient.CreateResponseAsync(
            BuildResponsePrompt(options, prompt),
            previousResponseId: null,
            cancellationToken);
        var outputText = response.Value.GetOutputText();

        if (string.IsNullOrWhiteSpace(outputText))
        {
            throw new SilverNormalizationException("Foundry response was empty.");
        }

        return outputText;
    }

    private static string BuildResponsePrompt(FoundryOrganizationOptions options, string prompt)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            options.EffectiveInstructions,
            prompt);
    }
}
