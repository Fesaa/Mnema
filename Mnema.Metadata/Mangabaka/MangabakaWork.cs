using System.Text.Json.Serialization;

namespace Mnema.Metadata.Mangabaka;

internal sealed record MangabakaWork
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("sub_title")]
    public string? SubTitle { get; init; }

    [JsonPropertyName("sequence_string")]
    public string? SequenceString { get; init; }

    [JsonPropertyName("sequence_numeric")]
    public float? SequenceNumeric { get; init; }

    [JsonPropertyName("part_of_volume")]
    public string? PartOfVolume { get; init; }

    [JsonPropertyName("count_type")]
    public string? CountType { get; init; }

    [JsonPropertyName("release_date")]
    public DateOnly? ReleaseDate { get; init; }

    [JsonPropertyName("description")]
    public DescriptionInfo? Description { get; init; }

    [JsonPropertyName("identifiers")]
    public List<IdentifierInfo>? Identifiers { get; init; }

    [JsonPropertyName("images")]
    public List<MangabakaCover>? Images { get; init; }

    internal sealed record DescriptionInfo
    {
        [JsonPropertyName("desc")]
        public string? Desc { get; init; }
    }

    internal sealed record IdentifierInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
