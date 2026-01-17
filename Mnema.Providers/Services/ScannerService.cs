using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Providers.Services;

public class ScannerService(
    ILogger<ScannerService> logger,
    IFileSystem fileSystem,
    IParserService parserService,
    ApplicationConfiguration configuration,
    INamingService namingService
    ) : IScannerService
{
    public List<OnDiskContent> ScanDirectoryAsync(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken)
    {
        var fullPath = Path.Join(configuration.BaseDir, path);
        if (!fileSystem.Directory.Exists(fullPath)) return [];

        var extensions = parserService.FileExtensionsForFormat(format);
        var contents = new List<OnDiskContent>();

        foreach (var entry in fileSystem.Directory.EnumerateFileSystemEntries(fullPath))
        {
            if (cancellationToken.IsCancellationRequested) return [];

            if (fileSystem.Directory.Exists(entry))
            {
                contents.AddRange(ScanDirectoryAsync(entry, contentFormat, format, cancellationToken));
                continue;
            }

            var extension = Path.GetExtension(entry);
            if (!extensions.IsMatch(extension)) continue;

            var content = ParseContent(entry, contentFormat);

            logger.LogTrace("Adding {FileName} to on disk content. (Vol. {Volume} Ch. {Chapter})", entry,
                content.Volume, content.Chapter);

            contents.Add(content);
        }

        return contents;
    }

    public OnDiskContent ParseContent(string file, ContentFormat contentFormat)
    {
        var series = parserService.ParseSeries(file, contentFormat);
        var volume = parserService.ParseVolume(file, contentFormat);
        var chapter = parserService.ParseChapter(file, contentFormat);

        return new OnDiskContent
        {
            SeriesName = series,
            Path = file,
            Volume = volume,
            Chapter = chapter
        };
    }
}
