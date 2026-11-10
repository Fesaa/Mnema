using System.Text.Json.Serialization;

namespace Mnema.Metadata.Mangabaka;

internal sealed record MangabakaCollection
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("series_id")]
    public int SeriesId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("medium")]
    public string? Medium { get; init; }

    [JsonPropertyName("count_main")]
    public int CountMain { get; init; }

    [JsonPropertyName("count_extra")]
    public int CountExtra { get; init; }

    [JsonPropertyName("count_other")]
    public int CountOther { get; init; }

    [JsonPropertyName("language")]
    public LanguageInfo? Language { get; init; }

    internal sealed record LanguageInfo
    {
        [JsonPropertyName("iso")]
        public string? Iso { get; init; }
    }
}
