using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Common.Helpers;
using Mnema.Models.Entities.Content;
using Mnema.Models.External;
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

    private static readonly XmlSerializer XmlSerializer = new(typeof(ComicInfo));
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
        var ci = ParseComicInfoFromFile(file);
        if (ci != null)
        {
            return new OnDiskContent
            {
                SeriesName = ci.Series,
                Path = file,
                Volume = ci.Volume,
                Chapter = ci.Number,
            };
        }

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

    private ComicInfo? ParseComicInfoFromFile(string file)
    {
        try
        {
            switch (parserService.ParseFormat(file))
            {
                case Format.Archive:
                    return ParseComicInfoFromArchive(file);
                case Format.Epub:
                    break;
                case Format.Unsupported:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static ComicInfo? ParseComicInfoFromArchive(string file)
    {
        using var archive = ZipFile.OpenRead(file);

        var comicInfoEntry = archive.GetEntry("ComicInfo.xml")??
                             archive.Entries
                                 .FirstOrDefault(e
                                     => e.Name.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase));
        if (comicInfoEntry == null) return null;

        return XmlHelper.Deserialize<ComicInfo>(XmlSerializer, comicInfoEntry.Open());
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

    public OnDiskContent? FindMatch(List<OnDiskContent> onDiskContents, Chapter chapter)
    {
        if (string.IsNullOrEmpty(chapter.VolumeMarker) && string.IsNullOrEmpty(chapter.ChapterMarker))
        {
            return null;
        }

        if (string.IsNullOrEmpty(chapter.ChapterMarker))
        {
            var volumeMatches = onDiskContents.Where(c => c.Volume == chapter.VolumeMarker).ToList();

            if (volumeMatches.Count == 1)
                return volumeMatches[0];

            return volumeMatches.FirstOrDefault(c => string.IsNullOrEmpty(c.Chapter));
        }

        if (string.IsNullOrEmpty(chapter.VolumeMarker))
        {
            return onDiskContents.FirstOrDefault(c
                => string.IsNullOrEmpty(c.Volume) && c.Chapter == chapter.ChapterMarker);
        }

        return onDiskContents.FirstOrDefault(c
            => c.Chapter == chapter.ChapterMarker && c.Volume == chapter.VolumeMarker);
    }
}
