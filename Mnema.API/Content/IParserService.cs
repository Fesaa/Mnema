using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public record NumberRange(string Value, float MinNumber, float MaxNumber);
public sealed record ParseResult(string Input, List<string> Series, NumberRange Volume, NumberRange Chapter);

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
