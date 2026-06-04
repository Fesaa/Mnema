using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Mnema.Metadata.Mangabaka;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


public enum MangabakaPublicationStatus
{
    [EnumMember(Value = "completed")]
    Completed,
    [EnumMember(Value = "releasing")]
    Releasing,
    [EnumMember(Value = "cancelled")]
    Cancelled,
    [EnumMember(Value = "hiatus")]
    Hiatus,
    [EnumMember(Value = "upcoming")]
    Upcoming,
    [EnumMember(Value = "unknown")]
    Unknown,
}

public static class MangabakaPublicationStatusExtensions
{
    public static bool HasFinalCount(this MangabakaPublicationStatus status)
    {
        return status is MangabakaPublicationStatus.Completed or MangabakaPublicationStatus.Cancelled;
    }
}

public class MangabakaPublisher
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    public static string Original = nameof(Original);
    public static string English = nameof(English);
}

public class MangabakaTitle
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("traits")]
    public List<string> Traits { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set;}
}

internal enum MangabakaTagWeight
{
    [EnumMember(Value = "core")]
    Core,
    [EnumMember(Value = "defining")]
    Defining,
    [EnumMember(Value = "recurrent")]
    Recurrent,
    [EnumMember(Value = "incidental")]
    Incidental,
    [EnumMember(Value = "unweighted")]
    Unweighted
}

internal enum MangabakaContentRating
{
    [EnumMember(Value = "safe")]
    [JsonStringEnumMemberName( "safe")]
    Safe,
    [EnumMember(Value = "suggestive")]
    [JsonStringEnumMemberName( "suggestive")]
    Suggestive,
    [EnumMember(Value = "erotica")]
    [JsonStringEnumMemberName( "erotica")]
    Erotica,
    [EnumMember(Value = "pornographic")]
    [JsonStringEnumMemberName( "pornographic")]
    Pornographic,
}

internal class MangabakaTagV2
{
    [JsonPropertyName("is_spoiler")]
    public bool IsSpoiler { get; set; }

    [JsonPropertyName("is_genre")]
    public bool IsGenre { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("weight")]
    public MangabakaTagWeight Weight { get; set; }

    [JsonPropertyName("content_rating")]
    public MangabakaContentRating ContentRating { get; set; }
}

[Table("series", Schema = "main")]
internal class MangabakaSeries
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("state")]
    public string? State { get; set; }

    [Column("merged_with")]
    public int? MergedWith { get; set; }

    [Column("titles")]
    public List<MangabakaTitle>? Titles { get; set; }

    [Column("published_start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("published_end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("published_start_date_is_estimated")]
    public bool? StartDateIsEstimated { get; set; }

    [Column("published_end_date_is_estimated")]
    public bool? EndDateIsEstimated { get; set; }

    [Column("romanized_title")]
    public string? RomanizedTitle { get; set; }

    [Column("secondary_titles_en")]
    public string? SecondaryTitlesEn { get; set; }

    [Column("cover_raw_url")]
    public string? CoverRawUrl { get; set; }

    [Column("cover_raw_size")]
    public int? CoverRawSize { get; set; }

    [Column("cover_raw_width")]
    public int? CoverRawWidth { get; set; }

    [Column("cover_raw_format")]
    public string? CoverRawFormat { get; set; }

    [Column("cover_raw_height")]
    public int? CoverRawHeight { get; set; }

    [Column("cover_raw_blurhash")]
    public string? CoverRawBlurhash { get; set; }

    [Column("cover_raw_thumbhash")]
    public string? CoverRawThumbhash { get; set; }

    [Column("cover_x150_x1")]
    public string? CoverX150X1 { get; set; }

    [Column("cover_x150_x2")]
    public string? CoverX150X2 { get; set; }

    [Column("cover_x150_x3")]
    public string? CoverX150X3 { get; set; }

    [Column("cover_x250_x1")]
    public string? CoverX250X1 { get; set; }

    [Column("cover_x250_x2")]
    public string? CoverX250X2 { get; set; }

    [Column("cover_x250_x3")]
    public string? CoverX250X3 { get; set; }

    [Column("cover_x350_x1")]
    public string? CoverX350X1 { get; set; }

    [Column("cover_x350_x2")]
    public string? CoverX350X2 { get; set; }

    [Column("cover_x350_x3")]
    public string? CoverX350X3 { get; set; }

    [Column("authors")]
    public List<string>? Authors { get; set; }

    [Column("artists")]
    public List<string>? Artists { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public MangabakaPublicationStatus Status { get; set; }

    [Column("is_licensed")]
    public int? IsLicensed { get; set; }

    [Column("has_anime")]
    public int? HasAnime { get; set; }

    [Column("anime")]
    public string? Anime { get; set; }

    [Column("content_rating")]
    public MangabakaContentRating? ContentRating { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("rating")]
    public float? Rating { get; set; }

    [Column("final_volume")]
    public string? FinalVolume { get; set; }

    [Column("final_chapter")]
    public string? FinalChapter { get; set; }

    [Column("total_chapters")]
    public string? TotalChapters { get; set; }

    [Column("links")]
    public List<string>? Links { get; set; }

    [Column("publishers")]
    public List<MangabakaPublisher>? Publishers { get; set; }

    [Column("relationships")]
    public string? Relationships { get; set; }

    [Column("genres")]
    public List<string>? Genres { get; set; }

    [Column("genres_v2")]
    public string? GenresV2 { get; set; }

    [Column("tags")]
    public string? Tags { get; set; }

    [Column("tags_v2")]
    public List<MangabakaTagV2> TagsV2 { get; set; }

    [Column("last_updated_at")]
    public string? LastUpdatedAt { get; set; }

    [Column("source_anilist_id")]
    public int? SourceAnilistId { get; set; }

    [Column("source_my_anime_list_id")]
    public string? SourceMyAnimeListId { get; set; }

    [Column("source_manga_updates_id")]
    public string? SourceMangaUpdatesId { get; set; }

    public List<string> CollectLinks()
    {
        var links = Links ?? [];

        if (SourceAnilistId != null)
        {
            links.Add($"https://anilist.co/manga/{SourceAnilistId}");
        }

        if (!string.IsNullOrEmpty(SourceMyAnimeListId))
        {
            links.Add($"https://myanimelist.net/manga/{SourceMyAnimeListId}");
        }

        if (!string.IsNullOrEmpty(SourceMangaUpdatesId))
        {
            links.Add($"https://www.mangaupdates.com/series/{SourceMangaUpdatesId}");
        }

        return links;
    }
}
