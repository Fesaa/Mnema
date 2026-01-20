using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Mnema.Models.Publication;

namespace Mnema.Providers.Weebdex;

internal record Response<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; }
    [JsonPropertyName("page")]
    public int Page { get; set; }
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

internal record Identifiable
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

internal sealed record TagResponse : Response<List<RelatedEntity>>;

internal sealed record SearchResponse: Response<List<Manga>>;

internal sealed record ChapterResponse : Response<List<WeebdexChapter>>;

internal enum ContentRating
{
    [JsonPropertyName("safe")] Safe,
    [JsonPropertyName("suggestive")] Suggestive,
    [JsonPropertyName("ertocia")] Erotica,
    [JsonPropertyName("pornographic")] Pornographic
}

internal enum Status
{
    [JsonPropertyName("ongoing")] Ongoing,
    [JsonPropertyName("completed")] Completed,
    [JsonPropertyName("hiatus")] Hiatus,
    [JsonPropertyName("cancelled")] Cancelled
}

internal static class EnumExtensions
{
    public static AgeRating AsAgeRating(this ContentRating contentRating)
    {
        return contentRating switch
        {
            ContentRating.Safe => AgeRating.Everyone,
            ContentRating.Suggestive => AgeRating.Teen,
            ContentRating.Erotica => AgeRating.Mature17Plus,
            ContentRating.Pornographic => AgeRating.AdultsOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(contentRating), contentRating, null)
        };
    }

    public static PublicationStatus AsPublicationStatus(this Status status)
    {
        return status switch
        {
            Status.Ongoing => PublicationStatus.Ongoing,
            Status.Completed => PublicationStatus.Completed,
            Status.Hiatus => PublicationStatus.Paused,
            Status.Cancelled => PublicationStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

internal enum Demographic
{
    [JsonPropertyName("shounen")] Shounen,
    [JsonPropertyName("shoujo")] Shoujo,
    [JsonPropertyName("josei")] Josei,
    [JsonPropertyName("seinen")] Seinen,
    [JsonPropertyName("none")] None
}

internal sealed record Manga : Identifiable
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("alt_titles")]
    public Dictionary<string, List<string>> AltTitles { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("content_rating")]
    public ContentRating ContentRating { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("demographic")]
    public string Demographic { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("last_chapter")]
    public string LastChapter { get; set; }

    [JsonPropertyName("last_volume")]
    public string LastVolume { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("relationships")]
    public MangaRelationships Relationships { get; set; }

    public string Size()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(LastVolume)) sb.Append($"{LastVolume} Vol.");

        if (!string.IsNullOrWhiteSpace(LastChapter)) sb.Append($"{LastChapter} Ch.");

        return sb.ToString();
    }

    public int? HighestChapter => string.IsNullOrWhiteSpace(LastChapter) ? null :
        int.TryParse(LastChapter, out var result) ? result : null;

    public int? HighestVolume => string.IsNullOrWhiteSpace(LastVolume) ? null :
        int.TryParse(LastVolume, out var result) ? result : null;

    public string? BestAltTitle(string language)
    {
        if (AltTitles.TryGetValue("ja", out var list) && list.Count > 0) return list[0];

        if (AltTitles.TryGetValue(language, out list) && list.Count > 0) return list[0];

        return null;
    }
}

internal sealed record MangaRelationships
{
    [JsonPropertyName("artists")]
    public List<RelatedEntity> Artists { get; set; }

    [JsonPropertyName("authors")]
    public List<RelatedEntity> Authors { get; set; }

    [JsonPropertyName("cover")]
    public Cover Cover { get; set; }

    [JsonPropertyName("links")]
    public Dictionary<string, string>? Links { get; set; }

    [JsonPropertyName("tags")]
    public List<RelatedEntity> Tags { get; set; }
}

internal sealed record RelatedEntity
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("group")]
    public string Group { get; set; }
}

internal sealed record Cover
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("dimensions")]
    public List<int> Dimensions { get; set; }

    [JsonPropertyName("ext")]
    public string Ext { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("volume")]
    public string Volume { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }
}


internal sealed record WeebdexChapter : Identifiable
{
    [JsonPropertyName("chapter")]
    public string? ChapterNumber { get; set; }

    [JsonPropertyName("volume")]
    public string? Volume { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("deleted_at")]
    public DateTime DeletedAt { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("is_unavailable")]
    public bool IsUnavailable { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("node")]
    public string Node { get; set; }

    [JsonPropertyName("data")]
    public List<PageData> Data { get; set; }

    [JsonPropertyName("data_optimized")]
    public List<PageData> DataOptimized { get; set; }

    [JsonPropertyName("relationships")]
    public ChapterRelationships Relationships { get; set; }
}

internal sealed record PageData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("dimensions")]
    public List<int> Dimensions { get; set; }
}

internal sealed record ChapterRelationships
{
    [JsonPropertyName("manga")]
    public Identifiable Manga { get; set; }

    [JsonPropertyName("groups")]
    public List<Group> Groups { get; set; }
}

internal sealed record Group
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("website")]
    public string Website { get; set; }

    [JsonPropertyName("discord")]
    public string Discord { get; set; }

    [JsonPropertyName("twitter")]
    public string Twitter { get; set; }

    [JsonPropertyName("mangaupdates")]
    public string MangaUpdates { get; set; }

    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }

    [JsonPropertyName("inactive")]
    public bool Inactive { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }
}



