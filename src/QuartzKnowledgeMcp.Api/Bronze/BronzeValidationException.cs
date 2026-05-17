namespace QuartzKnowledgeMcp.Api.Bronze;

public sealed class BronzeValidationException(IDictionary<string, string[]> errors)
    : Exception("Bronze source validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
