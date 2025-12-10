using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

internal sealed record SearchData: Identifiable
{
    public required MangaAttributes Attributes { get; set; }
    public required IList<RelationShip> RelationShips { get; set; }

    public string RefUrl => $"https://mangadex.org/title/{Id}/";
    
    public string? CoverUrl()
    {
        var cover = RelationShips.FirstOrDefault(r => r.Type == "cover_art");
        if (cover == null) return null;

        if (cover.Attributes.TryGetValue("fileName", out var fileName) && fileName.ValueKind == JsonValueKind.String)
        {
            return $"proxy/mangadex/covers/{Id}/{fileName}.256.jpg";
        }

        return null;
    }
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

internal sealed record MangadexSearchResponse: MangadexResponse<IList<SearchData>>;