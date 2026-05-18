namespace QuartzKnowledgeMcp.Api.Silver;

public sealed class BronzeSourceNotFoundException(Guid bronzeId)
    : Exception($"Bronze source '{bronzeId}' was not found.")
{
    public Guid BronzeId { get; } = bronzeId;
}

public sealed class SilverNormalizationException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class SilverValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Silver validation failed.")
{
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(errors);
}