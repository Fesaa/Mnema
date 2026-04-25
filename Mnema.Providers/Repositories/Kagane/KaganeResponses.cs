using System.Text.Json.Serialization;

namespace Mnema.Providers.Kagane;

public class IntegrityDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; }

    [JsonPropertyName("exp")]
    public long Expiration { get; set; }
}

public sealed record Genre
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("genre_name")]
    public string Name { get; set; }
    [JsonPropertyName("genre_type")]
    public string Type { get; set; }

    public bool IsActualGenre => Type == "genre";
}

public sealed record Tag
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("tag_name")]
    public string Name { get; set; }
}
