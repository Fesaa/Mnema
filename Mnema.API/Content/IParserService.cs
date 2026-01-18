using System.Text.RegularExpressions;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IParserService
{
    Regex FileExtensionsForFormat(Format format);

    string ParseSeries(string filename, ContentFormat type);
    string ParseVolume(string filename, ContentFormat type);
    string ParseChapter(string filename, ContentFormat type);
    bool IsDefaultChapter(string? chapterNumber);
    bool IsLooseLeafVolume(string? volumeNumber);
}
