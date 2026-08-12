using System;
using System.Collections.Generic;
using System.Globalization;
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
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.External;
using Mnema.Common.Extensions;
using Mnema.Common.Helpers;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.Scanner;
using Mnema.Models.Enums;
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
    IDistributedCache cache,
    IUnitOfWork unitOfWork
    ) : IScannerService
{

    private static readonly XmlSerializer XmlSerializer = new(typeof(ComicInfo));
    private static readonly BencodeParser BencodeParser = new();
    private static readonly StreamPipeReaderOptions StreamPipeReaderOptions = new();
    private static readonly DistributedCacheEntryOptions CacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1),
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

    private OnDiskContent ParseContent(string path, ContentFormat contentFormat)
    {
        var file = Path.GetFileName(path);

        var content = new OnDiskContent()
        {
            Path = path,
            FileName = file,
        };

        content.ComicInfo = ParseComicInfoFromFile(path);
        if (content.ComicInfo != null)
        {
            content.SeriesName = content.ComicInfo.Series;
            content.Volume = content.ComicInfo.Volume;
            content.Chapter = content.ComicInfo.Number;
        }

        if (string.IsNullOrEmpty(content.SeriesName))
            content.SeriesName = parserService.ParseSeries(file, contentFormat);

        if (string.IsNullOrEmpty(content.Volume))
        {
            var volume = parserService.ParseVolume(file, contentFormat);
            content.Volume = parserService.IsLooseLeafVolume(volume) ? string.Empty : volume;
        }

        if (string.IsNullOrEmpty(content.Chapter))
        {
            var chapter = parserService.ParseChapter(file, contentFormat);
            content.Chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter;
        }


        return content;
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
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to parse ComicInfo.xml from {FileName}", file);
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

    public async Task<ParsedTorrentInfo> ParseTorrentFile(string remoteUrl, CancellationToken cancellationToken)
    {
        var cacheKey = $"{remoteUrl}";

        var cached = await cache.GetAsJsonAsync<ParsedTorrentInfo>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        var stream = await httpClient.GetStreamAsync(remoteUrl, cancellationToken);

        var torrent = await BencodeParser.ParseAsync<Torrent>(stream, StreamPipeReaderOptions, cancellationToken);

        var files = torrent.FileMode switch
        {
            TorrentFileMode.Unknown => [],
            TorrentFileMode.Single => [
                new TorrentFile(torrent.DisplayName, Path.Join(torrent.DisplayName, torrent.File.FileName), torrent.TotalSize)
            ],
            TorrentFileMode.Multi => torrent.Files
                .Select(f => new TorrentFile(f.FileName, Path.Join(torrent.DisplayName, f.FullPath), f.FileSize))
                .ToList(),

            _ => throw new ArgumentOutOfRangeException(nameof(torrent.FileMode), torrent.FileMode, null)
        };

        var info = new ParsedTorrentInfo(torrent.TotalSize.AsHumanReadableSize(), files);

        await cache.SetAsJsonAsync(remoteUrl, info, CacheEntryOptions, cancellationToken);

        return info;
    }

    [Queue(HangfireQueue.ImportScanQueue)]
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 60)]
    public async Task ScanRoot(string path, CancellationToken cancellationToken)
    {
        if (await unitOfWork.ImportScanRepository.HasNonFinishedScan(path, cancellationToken))
        {
            logger.LogWarning("Scan already in progress for {Path}. Not processing further", path);
            return;
        }

        var toSkip = await unitOfWork.ImportScanRepository.GetAlreadyLinkedDirectoriesForRoot(path, cancellationToken);

        var importScan = new ImportScan
        {
            RootDir = path,
            Status = ImportScanStatus.Started,
            DirectoryImportResults = [],
            ImportErrors = [],
            StartedUtc = DateTime.UtcNow,
        };

        unitOfWork.ImportScanRepository.Add(importScan);
        await unitOfWork.CommitAsync(cancellationToken);

        try
        {
            await ScanDirectory(importScan, fileSystem.Path.Join(configuration.BaseDir, path), toSkip, cancellationToken);
            importScan.Status = ImportScanStatus.Finished;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scan failed for {Path}", path);
            importScan.Status = ImportScanStatus.Failed;
        }
        finally
        {
            importScan.FinishedUtc = DateTime.UtcNow;
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    private async Task ScanDirectory(ImportScan scan, string path, HashSet<string> alreadyScanned, CancellationToken cancellationToken)
    {
        if (!fileSystem.Directory.Exists(path))
        {
            logger.LogWarning("Directory {Path} does not exist, cannot process scan further", path);
            scan.ImportErrors.Add(ImportError.UnknownDirectory(path.RemovePrefix(configuration.BaseDir)));
            return;
        }

        var idx = 0;

        foreach (var directory in fileSystem.Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly))
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (alreadyScanned.Contains(directory)) continue;

            try
            {
                await ProcessDirectory(scan, directory, alreadyScanned, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process directory {Path}", directory);
                scan.ImportErrors.Add(ImportError.FromException(directory.RemovePrefix(configuration.BaseDir), ex));
            }

            if (idx++ % 5 == 0)
            {
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task ProcessDirectory(ImportScan scan, string path, HashSet<string> alreadyScanned, CancellationToken cancellationToken)
    {

        var files = fileSystem.Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
            .Where(parserService.IsSupportedFile)
            .ToList();
        if (files.Count == 0)
        {
            logger.LogDebug("No files found in directory {Path}, scanning subdirectories", path);
            await ScanDirectory(scan, path, alreadyScanned, cancellationToken);
            return;
        }

        var extensions = files.Select(f => fileSystem.Path.GetExtension(f)).Distinct().ToList();
        if (extensions.Count > 1)
        {
            logger.LogWarning("Directory {Path} contains files with different extensions: {Extensions}", path, extensions);
            scan.ImportErrors.Add(ImportError.MixedContentFormats(path.RemovePrefix(configuration.BaseDir), extensions));
            return;
        }

        var contentFormat = extensions[0].ContentFormatFromFileExt();
        if (contentFormat is null)
        {
            logger.LogWarning("Directory {Path} contains files with unknown content format: {Extension}", path, extensions[0]);
            scan.ImportErrors.Add(ImportError.FailedToParseContentFormat(path.RemovePrefix(configuration.BaseDir), files[0].RemovePrefix(path)));
            return;
        }

        var onDiskContent = ParseContent(files[0], contentFormat.Value);
        if (string.IsNullOrEmpty(onDiskContent.SeriesName))
        {
            logger.LogWarning("Directory {Path} contains files with unknown series name: {Extension}", path, extensions[0]);
            scan.ImportErrors.Add(ImportError.FailedToParseSeries(path.RemovePrefix(configuration.BaseDir), files[0].RemovePrefix(path)));
            return;
        }

        logger.LogDebug("Adding directory {Path} to scan. Series: {SeriesName}", path, onDiskContent.SeriesName);
        scan.DirectoryImportResults.Add(new DirectoryImportResult
        {
            Directory = path.RemovePrefix(configuration.BaseDir),
            Status = DirectoryImportStatus.Queued,
            ParsedSeriesName = onDiskContent.SeriesName,
            ParsedHardcoverId = ExternalIdParser.GetHardcoverSeriesId(onDiskContent.ComicInfo?.Web),
            ParsedMangaBakaId = ExternalIdParser.GetMangaBakaId(onDiskContent.ComicInfo?.Web),
            Files = files,
        });
    }


}
