using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Mnema.Common.Helpers;
using Mnema.Models.Publication;

namespace Mnema.Providers.Comix;

internal class ComixRespose<T>
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
    public bool Success => Status == "ok";
    [JsonPropertyName("code")]
    public int? Code { get; set; }
    [JsonPropertyName("result")]
    public T Result { get; set; }
}

internal class ComixPaginatedResult<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; }
    [JsonPropertyName("meta")]
    public ComixPagination Pagination { get; set; }
}

internal class ComixPagination
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("perPage")]
    public int PageSize { get; set; }
    [JsonPropertyName("lastPage")]
    public int TotalPages { get; set; }
}

internal class ComixManga
{
    [JsonPropertyName("hid")]
    public string HashId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("altTitles")]
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
    public ComixPoster? Poster { get; set; }

    [JsonPropertyName("originalLanguage")]
    public string OriginalLanguage { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("finalVolume")]
    public float? FinalVolume { get; set; }

    [JsonPropertyName("finalChapter")]
    public float? FinalChapter { get; set; }

    [JsonPropertyName("latestChapter")]
    public float? LatestChapter { get; set;}

    [JsonPropertyName("links")]
    public ComixLinks Links { get; set; }

    [JsonPropertyName("contentRating")]
    public string ContentRating { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("genres")]
    public List<ComixInclude> Genres { get; set; }

    [JsonPropertyName("authors")]
    public List<ComixInclude> Authors { get; set; }

    [JsonPropertyName("artists")]
    public List<ComixInclude> Artists { get; set; }

    public string Size()
    {
        var sb = new StringBuilder();

        if (FinalVolume is > 0) sb.Append($"{FinalVolume} Vol.");
        if (FinalChapter is > 0) sb.Append($"{FinalChapter} Ch.");

        return sb.ToString();
    }

    public AgeRating AsAgeRating()
    {
        return ContentRating switch
        {
            "safe" => AgeRating.Everyone,
            "suggestive" => AgeRating.Teen,
            "erotica" => AgeRating.Mature17Plus,
            "pornographic" => AgeRating.AdultsOnly,
            "" or null => AgeRating.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(ContentRating), ContentRating, null)
        };
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
    [JsonPropertyName("id")]
    public int ChapterId { get; set; }

    [JsonPropertyName("isOfficial")]
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

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public long UpdatedAt { get; set; }

    [JsonPropertyName("scanlationGroup")]
    public ComixScanlationGroup? ScanlationGroup { get; set; }

    [JsonPropertyName("pages")]
    public List<ComixImage> Images { get; set; }
}

internal class ComixScanlationGroup
{

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
