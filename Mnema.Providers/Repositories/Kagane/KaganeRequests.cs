using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mnema.Providers.Kagane;

public sealed record KaganeSearchRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("content_rating")]
    public List<string> ContentRating { get; set; }

    [JsonPropertyName("genres")]
    public KaganaSearchRequestFilter Genres { get; set; }

    [JsonPropertyName("tags")]
    public KaganaSearchRequestFilter Tags { get; set; }

    [JsonPropertyName("source_id")]
    public List<string> SourceId { get; set; }

    [JsonPropertyName("source_type")]
    public List<string> SourceType { get; set; }

    [JsonPropertyName("upload_status")]
    public List<string> UploadStatus { get; set; }

    [JsonPropertyName("publication_status")]
    public List<string> PublicationStatus { get; set;}

}

public sealed record KaganaSearchRequestFilter
{
    [JsonPropertyName("exclude")]
    public List<string> Exclude { get; set; }

    [JsonPropertyName("values")]
    public List<string> Values { get; set; }

    [JsonPropertyName("match_all")]
    public bool MatchAll { get; set; }
}
