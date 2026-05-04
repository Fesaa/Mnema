using System.Text.Json.Serialization;

namespace Mnema.Metadata.Mangabaka;

internal sealed record PaginatedResponse<T>
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    [JsonPropertyName("data")]
    public List<T>? Data { get; set; }
    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; set; }
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

internal sealed record Pagination
{
    [JsonPropertyName("count")]
    public int Total { get; set; }
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

internal sealed record MangabakaCover
{
    [JsonPropertyName("language")]
    public string Language { get; init; }
    [JsonPropertyName("index")]
    public string Index { get; init; }
    [JsonPropertyName("type")]
    public string Type { get; init; }
    [JsonPropertyName("image")]
    public MangabakaCoverImage Image { get; init; }
}

internal sealed record MangabakaCoverImage
{
    [JsonPropertyName("raw")]
    public MangabakaCoverRawImage RawImage { get; init; }
}

internal sealed record MangabakaCoverRawImage
{
    [JsonPropertyName("url")]
    public string Url { get; init; }
    [JsonPropertyName("format")]
    public string Format { get; init; }
}
