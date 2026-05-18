using System.Text.Json;

namespace QuartzKnowledgeMcp.Api.Gold;

public static class GoldCatalogJson
{
    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    public static IReadOnlyList<T> DeserializeList<T>(string json)
    {
        return JsonSerializer.Deserialize<List<T>>(json)
            ?? [];
    }
}