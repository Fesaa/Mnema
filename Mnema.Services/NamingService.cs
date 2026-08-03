using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.Entities;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class NamingService(ILogger<NamingService> logger, ApplicationConfiguration configuration, IParserService parserService) : INamingService
{
    public StringFormatter<ChapterNameContext> ChapterFormatter { get; } = new StringFormatter<ChapterNameContext>()
        .WithVariable("Title", c => c.Title)
        .WithVariable("Volume", c => parserService.IsLooseLeafVolume(c.Chapter.VolumeMarker) ? null : c.Chapter.VolumeMarker)
        .WithVariable("Chapter", (c, spec) =>
        {
            if (parserService.IsDefaultChapter(c.Chapter.ChapterMarker))
                return null;

            var number = c.Chapter.ChapterNumber();
            if (number is null)
            {
                logger.LogWarning("Failed to parse chapter number for marker {ChapterMarker}, not padding", number);
                return null;
            }

            return spec is not null ? c.Chapter.ChapterMarker.PadFloat(spec.Length) : c.Chapter.ChapterMarker;
        })
        .WithVariable("ChapterTitle", c => c.Chapter.Title)
        .WithVariable("Year", c => c.Chapter.ReleaseDate?.Year.ToString() ?? string.Empty)
        .WithVariable("Date", (c, spec) => c.Chapter.ReleaseDate?.ToString(spec ?? "yyyy-MM-dd") ?? string.Empty);

    public StringFormatter<ChapterNameContext> OneShotFormatter => ChapterFormatter;

    public string GetVolumeDirectoryName(string title, string volumeMarker)
        => $"{title} Vol. {volumeMarker}";

    public string GetChapterFilePath(string baseDir, string title, string fileName)
        => Path.Join(configuration.DownloadDir, baseDir, title, fileName);

    public string GetChapterFileName(Preferences preferences, string title, Chapter chapter,
        IReadOnlyCollection<string>? existingPaths = null)
    {
        var ctx = new ChapterNameContext(title, chapter);

        var format = chapter.IsOneShot ? preferences.OneShotFileFormat : preferences.ChapterFileFormat;
        if (string.IsNullOrEmpty(format) || !ChapterFormatter.IsValid(format))
        {
            logger.LogWarning("Empty or invalid ");
            format = chapter.IsOneShot ? INamingService.DefaultOneShotFormat : INamingService.DefaultChapterFormat;
        }

        var fileName = ChapterFormatter.Apply(format, ctx);

        return chapter.IsOneShot ? EnsureUnique(fileName, chapter.Title, existingPaths ?? []) : fileName;
    }

    private string EnsureUnique(string fileName, string title, IReadOnlyCollection<string> existingPaths)
    {
        var idx = 0;
        var finalFileName = fileName;

        while (existingPaths.Contains(finalFileName))
        {
            finalFileName = $"{fileName} ({idx})";

            if (idx >= 25)
            {
                logger.LogWarning("More than 25 one shots with the same name for {Title}, generating random number", title);
                finalFileName = $"{fileName} ({Random.Shared.Next()})";
                break;
            }

            idx++;
        }

        return finalFileName;
    }
}
