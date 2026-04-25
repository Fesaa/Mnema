using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Mnema.Common.Helpers;

namespace Mnema.Providers.Comix;

internal class ComixRespose<T>
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    [JsonPropertyName("result")]
    public T Result { get; set; }
}

internal class ComixPaginatedResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; }
    [JsonPropertyName("pagination")]
    public ComixPagination Pagination { get; set; }
}

internal class ComixPagination
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("per_page")]
    public int PageSize { get; set; }
    [JsonPropertyName("last_page")]
    public int TotalPages { get; set; }
}

internal class ComixManga
{
    [JsonPropertyName("manga_id")]
    public int MangaId { get; set; }

    [JsonPropertyName("hash_id")]
    public string HashId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("alt_titles")]
    public List<string> AltTitles { get; set; }

    [JsonPropertyName("synopsis")]
    public string Synopsis { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("poster")]
    public ComixPoster Poster { get; set; }

    [JsonPropertyName("original_language")]
    public string OriginalLanguage { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("final_volume")]
    public float? FinalVolume { get; set; }

    [JsonPropertyName("final_chapter")]
    public float? FinalChapter { get; set; }

    [JsonPropertyName("has_chapters")]
    public bool HasChapters { get; set; }

    [JsonPropertyName("latest_chapter")]
    public float? LatestChapter { get; set;}

    [JsonPropertyName("links")]
    public ComixLinks Links { get; set; }

    [JsonPropertyName("is_nsfw")]
    public bool IsNsfw { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("term_ids")]
    public List<int> TermIds { get; set; }

    [JsonPropertyName("genre")]
    public List<ComixInclude> Genres { get; set; }

    [JsonPropertyName("author")]
    public List<ComixInclude> Authors { get; set; }

    [JsonPropertyName("artist")]
    public List<ComixInclude> Artists { get; set; }

    public string Size()
    {
        var sb = new StringBuilder();

        if (FinalVolume is > 0) sb.Append($"{FinalVolume} Vol.");
        if (FinalChapter is > 0) sb.Append($"{FinalChapter} Ch.");

        return sb.ToString();
    }
}


internal class ComixPoster
{
    [JsonPropertyName("small")]
    public string Small { get; set; }

    [JsonPropertyName("medium")]
    public string Medium { get; set; }

    [JsonPropertyName("large")]
    public string Large { get; set; }
}

internal class ComixLinks
{
    [JsonPropertyName("al")]
    public string Al { get; set; }

    [JsonPropertyName("mal")]
    public string Mal { get; set; }

    [JsonPropertyName("mu")]
    public string Mu { get; set; }

    public List<string> Links()
    {
        var res = new List<string>();

        if (!string.IsNullOrEmpty(Al)) res.Add(Al);
        if (!string.IsNullOrEmpty(Mal)) res.Add(Mal);
        if (!string.IsNullOrEmpty(Mu)) res.Add(Mu);

        return res;
    }
}

internal class ComixInclude
{
    [JsonPropertyName("term_id")]
    public int Id { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}

internal class ComixChapter
{
    [JsonPropertyName("chapter_id")]
    public int ChapterId { get; set; }

    [JsonPropertyName("manga_id")]
    public int MangaId { get; set; }

    [JsonPropertyName("scanlation_group_id")]
    public int ScanlationGroupId { get; set; }

    [JsonPropertyName("is_official")]
    [JsonConverter(typeof(FlexibleBooleanConverter))]
    public bool IsOfficial { get; set; }

    [JsonPropertyName("number")]
    public decimal Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("volume")]
    public int Volume { get; set; }

    [JsonPropertyName("votes")]
    public int Votes { get; set; }

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    [JsonPropertyName("scanlation_group")]
    public ComixScanlationGroup? ScanlationGroup { get; set; }

    [JsonPropertyName("images")]
    public List<ComixImage> Images { get; set; }
}

internal class ComixScanlationGroup
{
    [JsonPropertyName("scanlation_group_id")]
    public int ScanlationGroupId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}

internal class ComixImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}
