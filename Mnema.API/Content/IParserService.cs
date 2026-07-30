using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public record NumberRange(string Value, float MinNumber, float MaxNumber);

public sealed record ParseResult(string Input, List<string> Series, NumberRange Volume, NumberRange Chapter)
    : IHasPositionMarkers
{
    public string VolumeMarker => Volume.Value;
    public string ChapterMarker => Chapter.Value;

    public override string ToString()
    {
        var seriesName = Series.Count > 0 ? string.Join(" ", Series) : "<No Series>";
        return $"ParseResult[{seriesName} | Vol. {Volume.Value} | Ch. {Chapter.Value}]";
    }
}

public static class ParseResultGrouping
{
    public static List<(List<string> Series, List<ParseResult> Items)> GroupMergingSeries(this IEnumerable<ParseResult> results)
    {
        var list = results.ToList();

        var parent = new Dictionary<string, string>();

        foreach (var r in list)
        {
            if (r.Series.Count == 0) continue;
            var first = r.Series[0];
            Find(first);

            foreach (var s in r.Series.Skip(1))
                Union(first, s);
        }

        var groups = list
            .GroupBy(r => r.Series.Count > 0 ? Find(r.Series[0]) : "")
            .Select(g => (
                Series: g.SelectMany(r => r.Series).Distinct().ToList(),
                Items: g.ToList()
            ))
            .ToList();

        return groups;

        void Union(string a, string b)
        {
            var ra = Find(a);
            var rb = Find(b);
            if (ra != rb)
                parent[ra] = rb;
        }

        string Find(string x)
        {
            parent.TryAdd(x, x);
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }
    }
}

public interface IParserService
{
    Regex FileExtensionsForFormat(Format format);

    string ParseSeries(string filename, ContentFormat type);
    /// <summary>
    /// To be used in congjuction with <see cref="ParseSeries"/> in case JA | EN is used
    /// </summary>
    /// <param name="series"></param>
    /// <returns></returns>
    List<string> ParseSeriesCollection(string series);
    string ParseVolume(string filename, ContentFormat type);
    string ParseChapter(string filename, ContentFormat type);
    bool IsDefaultChapter(string? chapterNumber);
    bool IsLooseLeafVolume(string? volumeNumber);
    bool IsCoverImage(string filename);
    bool IsImage(string filePath);
    float MinNumberFromRange(string range);
    float MaxNumberFromRange(string range);
    ParseResult FullParse(string input, ContentFormat type);
    Format ParseFormat(string filePath);
}
