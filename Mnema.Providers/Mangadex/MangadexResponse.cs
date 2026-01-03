using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mnema.Common.Extensions;
using Mnema.Models.Publication;

namespace Mnema.Providers.Mangadex;

internal sealed class LanguageMap: Dictionary<string, string>;

internal record MangadexResponse<TResponse>
{
    public string Result { get; set; }
    public string Response { get; set; }
    public TResponse Data { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int Total { get; set; }
}

internal record Identifiable
{
    public required string Id { get; set; }
    public required string Type { get; set; }
}

internal sealed record MangaData: Identifiable
{
    public required MangaAttributes Attributes { get; set; }
    public required IList<RelationShip> RelationShips { get; set; }

    public string RefUrl => $"https://mangadex.org/title/{Id}/";
    
    public string? CoverUrl(bool proxy = true)
    {
        var cover = RelationShips.FirstOrDefault(r => r.Type == "cover_art");
        if (cover == null) return null;

        if (cover.Attributes.TryGetValue("fileName", out var fileName) && fileName.ValueKind == JsonValueKind.String)
        {
            return proxy ? $"proxy/mangadex/covers/{Id}/{fileName}.256.jpg" : $"https://mangadex.org/covers/{Id}/{fileName}.256.jpg";
        }

        return null;
    }

    private static readonly Dictionary<string, PersonRole[]> RelationMappings = new()
    {
        ["author"] = [PersonRole.Writer],
        ["artist"] = [PersonRole.Colorist],
    };

    public IList<Person> People => RelationShips.Select(r =>
    {
        if (!r.Attributes.TryGetValue("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var name = nameEl.GetString();
        if (string.IsNullOrEmpty(name)) return null;
        
        if (RelationMappings.TryGetValue(r.Type, out var roles))
        {
            return new Person
            {
                Name = name,
                Roles = roles,
            };
        }

        return null;
    }).WhereNotNull().ToList();
}

internal sealed record MangaAttributes
{
    public required LanguageMap Title { get; set; }
    public required IList<LanguageMap> AltTitles { get; set; }
    public required LanguageMap Description { get; set; }
    public required bool IsLocked { get; set; }
    public required IDictionary<string, string> Links { get; set; }
    public required string OriginalLanguage { get; set; }
    public string? LastVolume { get; set; }
    public string? LastChapter { get; set; }
    public required Status Status { get; set; }
    public int? Year { get; set; }
    public required ContentRating ContentRating { get; set; }
    public required IList<TagData> Tags { get; set; }

    public string LangTitle(string lang)
    {
        // Note: for some reason the en title may still be in Japanese, don't really have a way of checking if it is
        // as the Japanese title is in the latin alphabet. We'll just have to be fine with it, as the alternative titles
        // are just plain weird from time to time
        if (Title.TryGetValue(lang, out var title) && !string.IsNullOrEmpty(title))
        {
            return title;
        }

        foreach (var altTitleMap in AltTitles)
        {
            if (altTitleMap.TryGetValue(lang, out var altTitle) && !string.IsNullOrEmpty(altTitle))
            {
                return altTitle;
            }
        }

        return Title.Values.FirstOrDefault(t => !string.IsNullOrEmpty(t)) ?? "Mnema-Fallback-Title";
    }

    public string Size()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(LastVolume))
        {
            sb.Append($"{LastVolume} Vol.");
        }

        if (!string.IsNullOrWhiteSpace(LastChapter))
        {
            sb.Append($"{LastChapter} Ch.");
        }

        return sb.ToString();
    }

    public int? HighestChapter => string.IsNullOrWhiteSpace(LastChapter) ? null :
        int.TryParse(LastChapter, out var result) ? result : null;
    
    public int? HighestVolume => string.IsNullOrWhiteSpace(LastVolume) ? null :
        int.TryParse(LastVolume, out var result) ? result : null;

}

internal sealed record ChapterData: Identifiable
{
    public required ChapterAttributes Attributes { get; set; }
    
    public required IList<RelationShip> RelationShips { get; set; }
}

internal sealed record ChapterAttributes
{
    public string Title { get; set; } = string.Empty; 
    public string? Volume { get; set; } = string.Empty;
    public string? Chapter { get; set; } = string.Empty;
    public string TranslatedLanguage { get; set; } = string.Empty;
    public string ExternalUrl { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public DateTime ReadableAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

internal enum Status
{
    [JsonPropertyName("ongoing")]
    Ongoing,
    [JsonPropertyName("completed")]
    Completed,
    [JsonPropertyName("hiatus")]
    Hiatus,
    [JsonPropertyName("cancelled")]
    Cancelled,
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

internal enum ContentRating
{
    [JsonPropertyName("safe")]
    Safe,
    [JsonPropertyName("suggestive")]
    Suggestive,
    [JsonPropertyName("ertocia")]
    Erotica,
    [JsonPropertyName("pornographic")]
    Pornographic,
}

internal sealed record TagData: Identifiable
{
    public required TagAttributes Attributes { get; set; }
}

internal sealed record TagAttributes
{
    public required LanguageMap Name { get; set; }
    public required LanguageMap Description { get; set; }
    public required string Group { get; set; }
    public required int Version { get; set; }
    public IList<RelationShip> RelationShips { get; set; } = [];
}

internal sealed record RelationShip: Identifiable
{
    public IDictionary<string, JsonElement> Attributes { get; set; } = new Dictionary<string, JsonElement>();
}

internal sealed record ChapterImagesResponse
{
    public required string Result { get; set; }
    public required string BaseUrl { get; set; }
    public required ChapterImagesInfo Chapter { get; set; }
}

internal sealed record ChapterImagesInfo
{
    public required string Hash { get; set; }
    public required IList<string> Data { get; set; }
}

internal sealed record CoverData: Identifiable
{
    public CoverAttributes Attributes { get; set; }
    public IList<RelationShip> RelationShips { get; set; } = [];

    public string Url(string seriesId) => $"https://uploads.mangadex.org/covers/{seriesId}/{Attributes.FileName}.512.jpg";
}

internal sealed record CoverAttributes
{
    public string Descritpion { get; set; }
    public string Volume { get; set; }
    public string FileName { get; set; }
    public string Locale { get; set; }
}

internal sealed record SearchResponse: MangadexResponse<IList<MangaData>>;

internal sealed record MangaResponse: MangadexResponse<MangaData>;

internal sealed record ChaptersResponse: MangadexResponse<List<ChapterData>>;

internal sealed record TagResponse: MangadexResponse<List<TagData>>;

internal sealed record CoverResponse: MangadexResponse<List<CoverData>>;