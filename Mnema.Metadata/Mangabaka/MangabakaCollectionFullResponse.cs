using System.Text.Json.Serialization;

namespace Mnema.Metadata.Mangabaka;

internal sealed record MangabakaCollectionFullResponse
{
    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("data")]
    public DataInfo? Data { get; init; }

    internal sealed record DataInfo
    {
        [JsonPropertyName("works")]
        public List<MangabakaWork>? Works { get; init; }
    }
}
