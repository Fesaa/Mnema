using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Providers.Services;

public class ScannerService(
    ILogger<ScannerService> logger,
    IFileSystem fileSystem,
    IParserService parserService,
    ApplicationConfiguration configuration,
    INamingService namingService,
    HttpClient httpClient,
    IDistributedCache cache
    ) : IScannerService
{

    private static readonly BencodeParser BencodeParser = new();
    private static readonly StreamPipeReaderOptions StreamPipeReaderOptions = new();
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
    };

    public List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
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
                contents.AddRange(ScanDirectory(entry, contentFormat, format, cancellationToken));
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
            Volume = parserService.IsLooseLeafVolume(volume) ? string.Empty : volume,
            Chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter,
        };
    }

    public async Task<List<Chapter>> ParseTorrentFile(string remoteUrl, ContentFormat contentFormat, CancellationToken cancellationToken)
    {
        var chapters = await cache.GetAsJsonAsync<List<Chapter>>(remoteUrl, cancellationToken);
        if (chapters != null)
            return chapters;

        var stream = await httpClient.GetStreamAsync(remoteUrl, cancellationToken);

        var torrent = await BencodeParser.ParseAsync<Torrent>(stream, StreamPipeReaderOptions, cancellationToken);

        chapters = torrent.FileMode switch
        {
            TorrentFileMode.Unknown => [],
            TorrentFileMode.Single => [
                ParseChapter(Path.Join(torrent.DisplayName, torrent.File.FileName), torrent.File.FileName, contentFormat)
            ],
            TorrentFileMode.Multi => torrent.Files
                .Select(f => ParseChapter(Path.Join(torrent.DisplayName, f.FullPath), f.FileName, contentFormat))
                .ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(torrent.FileMode), torrent.FileMode, null)
        };

        await cache.SetAsJsonAsync(remoteUrl, chapters, CacheEntryOptions, cancellationToken);

        return chapters;
    }

    private Chapter ParseChapter(string path, string file, ContentFormat contentFormat)
    {
        var volume = parserService.ParseVolume(file, contentFormat);
        var chapter = parserService.ParseChapter(file, contentFormat);

        volume = parserService.IsLooseLeafVolume(volume) ? string.Empty : volume;
        chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter;

        return new Chapter
        {
            Id = string.Empty,
            Title = path,
            VolumeMarker = volume,
            ChapterMarker = chapter,
            Tags = [],
            People = [],
            TranslationGroups = []
        };
    }
}
