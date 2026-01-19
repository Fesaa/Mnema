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
}

public class MangabakaPublisher
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
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

    [Column("title")]
    public string Title { get; set; }

    [Column("native_title")]
    public string? NativeTitle { get; set; }

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

    [Column("year")]
    public int? Year { get; set; }

    [Column("status")]
    public MangabakaPublicationStatus Status { get; set; }

    [Column("is_licensed")]
    public int? IsLicensed { get; set; }

    [Column("has_anime")]
    public int? HasAnime { get; set; }

    [Column("anime")]
    public string? Anime { get; set; }

    [Column("content_rating")]
    public string? ContentRating { get; set; }

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
    public string? TagsV2 { get; set; }

    [Column("last_updated_at")]
    public string? LastUpdatedAt { get; set; }

    [Column("source_anilist_id")]
    public int? SourceAnilistId { get; set; }

    [Column("source_anilist_rating")]
    public float? SourceAnilistRating { get; set; }

    [Column("source_anilist_rating_normalized")]
    public int? SourceAnilistRatingNormalized { get; set; }

    [Column("source_anilist_cover")]
    public string? SourceAnilistCover { get; set; }

    [Column("source_anilist_last_updated_at")]
    public string? SourceAnilistLastUpdatedAt { get; set; }

    [Column("source_anilist_response")]
    public string? SourceAnilistResponse { get; set; }

    [Column("source_anime_planet_id")]
    public string? SourceAnimePlanetId { get; set; }

    [Column("source_anime_planet_rating")]
    public int? SourceAnimePlanetRating { get; set; }

    [Column("source_anime_planet_rating_normalized")]
    public int? SourceAnimePlanetRatingNormalized { get; set; }

    [Column("source_anime_planet_cover")]
    public string? SourceAnimePlanetCover { get; set; }

    [Column("source_anime_planet_last_updated_at")]
    public string? SourceAnimePlanetLastUpdatedAt { get; set; }

    [Column("source_anime_planet_response")]
    public string? SourceAnimePlanetResponse { get; set; }

    [Column("source_shikimori_id")]
    public string? SourceShikimoriId { get; set; }

    [Column("source_shikimori_rating")]
    public string? SourceShikimoriRating { get; set; }

    [Column("source_shikimori_rating_normalized")]
    public string? SourceShikimoriRatingNormalized { get; set; }

    [Column("source_shikimori_cover")]
    public string? SourceShikimoriCover { get; set; }

    [Column("source_shikimori_last_updated_at")]
    public string? SourceShikimoriLastUpdatedAt { get; set; }

    [Column("source_shikimori_response")]
    public string? SourceShikimoriResponse { get; set; }

    [Column("source_anime_news_network_id")]
    public string? SourceAnimeNewsNetworkId { get; set; }

    [Column("source_anime_news_network_rating")]
    public string? SourceAnimeNewsNetworkRating { get; set; }

    [Column("source_anime_news_network_rating_normalized")]
    public string? SourceAnimeNewsNetworkRatingNormalized { get; set; }

    [Column("source_anime_news_network_cover")]
    public string? SourceAnimeNewsNetworkCover { get; set; }

    [Column("source_anime_news_network_last_updated_at")]
    public string? SourceAnimeNewsNetworkLastUpdatedAt { get; set; }

    [Column("source_anime_news_network_response")]
    public string? SourceAnimeNewsNetworkResponse { get; set; }

    [Column("source_manga_updates_id")]
    public string? SourceMangaUpdatesId { get; set; }

    [Column("source_manga_updates_rating")]
    public float? SourceMangaUpdatesRating { get; set; }

    [Column("source_manga_updates_rating_normalized")]
    public int? SourceMangaUpdatesRatingNormalized { get; set; }

    [Column("source_manga_updates_cover")]
    public string? SourceMangaUpdatesCover { get; set; }

    [Column("source_manga_updates_last_updated_at")]
    public string? SourceMangaUpdatesLastUpdatedAt { get; set; }

    [Column("source_manga_updates_response")]
    public string? SourceMangaUpdatesResponse { get; set; }

    [Column("source_my_anime_list_id")]
    public string? SourceMyAnimeListId { get; set; }

    [Column("source_my_anime_list_rating")]
    public string? SourceMyAnimeListRating { get; set; }

    [Column("source_my_anime_list_rating_normalized")]
    public string? SourceMyAnimeListRatingNormalized { get; set; }

    [Column("source_my_anime_list_cover")]
    public string? SourceMyAnimeListCover { get; set; }

    [Column("source_my_anime_list_last_updated_at")]
    public string? SourceMyAnimeListLastUpdatedAt { get; set; }

    [Column("source_my_anime_list_response")]
    public string? SourceMyAnimeListResponse { get; set; }

    [Column("source_kitsu_id")]
    public int? SourceKitsuId { get; set; }

    [Column("source_kitsu_rating")]
    public string? SourceKitsuRating { get; set; }

    [Column("source_kitsu_rating_normalized")]
    public string? SourceKitsuRatingNormalized { get; set; }

    [Column("source_kitsu_cover")]
    public string? SourceKitsuCover { get; set; }

    [Column("source_kitsu_last_updated_at")]
    public string? SourceKitsuLastUpdatedAt { get; set; }

    [Column("source_kitsu_response")]
    public string? SourceKitsuResponse { get; set; }

    [Column("relationships_other")]
    public string? RelationshipsOther { get; set; }

    [Column("relationships_adaptation")]
    public string? RelationshipsAdaptation { get; set; }

    [Column("relationships_prequel")]
    public string? RelationshipsPrequel { get; set; }

    [Column("relationships_main_story")]
    public string? RelationshipsMainStory { get; set; }

    [Column("relationships_sequel")]
    public string? RelationshipsSequel { get; set; }

    [Column("anime_end")]
    public string? AnimeEnd { get; set; }

    [Column("anime_start")]
    public string? AnimeStart { get; set; }

    [Column("relationships_alternative")]
    public string? RelationshipsAlternative { get; set; }

    [Column("secondary_titles_ko")]
    public string? SecondaryTitlesKo { get; set; }

    [Column("relationships_side_story")]
    public string? RelationshipsSideStory { get; set; }

    [Column("relationships_spin_off")]
    public string? RelationshipsSpinOff { get; set; }

    [Column("secondary_titles_ja-ro")]
    public string? SecondaryTitlesJaRo { get; set; }

    [Column("secondary_titles_ja")]
    public string? SecondaryTitlesJa { get; set; }

    [Column("secondary_titles_es")]
    public string? SecondaryTitlesEs { get; set; }

    [Column("secondary_titles_zh-ro")]
    public string? SecondaryTitlesZhRo { get; set; }

    [Column("secondary_titles_ko-ro")]
    public string? SecondaryTitlesKoRo { get; set; }

    [Column("secondary_titles_vi")]
    public string? SecondaryTitlesVi { get; set; }

    [Column("secondary_titles_zh")]
    public string? SecondaryTitlesZh { get; set; }

    [Column("secondary_titles_es-la")]
    public string? SecondaryTitlesEsLa { get; set; }

    [Column("secondary_titles_th")]
    public string? SecondaryTitlesTh { get; set; }

    [Column("secondary_titles_pt")]
    public string? SecondaryTitlesPt { get; set; }

    [Column("secondary_titles_fr")]
    public string? SecondaryTitlesFr { get; set; }

    [Column("secondary_titles_zh-hk")]
    public string? SecondaryTitlesZhHk { get; set; }

    [Column("secondary_titles_de")]
    public string? SecondaryTitlesDe { get; set; }

    [Column("secondary_titles_pt-br")]
    public string? SecondaryTitlesPtBr { get; set; }

    [Column("secondary_titles_ru")]
    public string? SecondaryTitlesRu { get; set; }

    [Column("secondary_titles_uk")]
    public string? SecondaryTitlesUk { get; set; }
}
