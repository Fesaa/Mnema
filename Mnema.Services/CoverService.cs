using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class CoverService(
    ILogger<CoverService> logger,
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    IFileSystem fileSystem,
    IParserService parserService,
    IUnitOfWork unitOfWork,
    ApplicationConfiguration configuration
) : ICoverService
{
    private const long MaxCoverSize = 25 * 1024 * 1024;
    private const string CoverPrefix = "!0000 cover";

    public async Task<List<Cover>> GetCovers(MonitoredSeries series, CancellationToken cancellationToken = default)
    {
        var covers = new List<Cover>();

        var repository = serviceProvider.GetKeyedService<IRepository>(series.Provider);
        if (repository is ICoverRepository coverRepository && !string.IsNullOrEmpty(series.ExternalId))
        {
            try
            {
                covers.AddRange(await coverRepository.GetCovers(new DownloadRequestDto
                {
                    Provider = series.Provider,
                    Id = series.ExternalId,
                    BaseDir = series.BaseDir,
                    TempTitle = series.Title,
                    Metadata = series.Metadata,
                }, cancellationToken));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to load covers from {Provider} for {Series}", series.Provider, series.Title);
            }
        }

        if (!string.IsNullOrEmpty(series.MangaBakaId))
        {
            var provider = serviceProvider.GetKeyedService<IMetadataProviderService>(MetadataProvider.Mangabaka);
            if (provider != null)
            {
                try
                {
                    covers.AddRange(await provider.GetCovers(series.MangaBakaId, cancellationToken));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load MangaBaka covers for {Series}", series.Title);
                }
            }
        }

        if (!string.IsNullOrEmpty(series.HardcoverId))
        {
            var provider = serviceProvider.GetKeyedService<IMetadataProviderService>(MetadataProvider.Hardcover);
            if (provider != null)
            {
                try
                {
                    covers.AddRange(await provider.GetCovers(series.HardcoverId, cancellationToken));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to load Hardcover covers for {Series}", series.Title);
                }
            }
        }

        return covers
            .Where(c => !string.IsNullOrWhiteSpace(c.Url))
            .GroupBy(c => c.Url, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(c => ParseVolume(c.Volume) ?? float.MaxValue)
            .ThenBy(c => c.Volume)
            .ToList();
    }

    public async Task<ArchiveCover?> GetArchiveCover(MonitoredChapter chapter,
        CancellationToken cancellationToken = default)
    {
        var path = ResolveArchivePath(chapter);
        if (path == null) return null;

        try
        {
            await using var stream = fileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var entry = FindCoverEntry(archive);
            if (entry == null) return null;

            if (entry.Length > MaxCoverSize)
            {
                logger.LogWarning("Cover entry {Entry} in {Path} is too large ({Size} bytes)",
                    entry.FullName, path, entry.Length);
                return null;
            }

            await using var entryStream = await entry.OpenAsync(cancellationToken);
            using var memory = new MemoryStream();
            await entryStream.CopyToAsync(memory, cancellationToken);

            return new ArchiveCover(memory.ToArray(), fileSystem.Path.GetExtension(entry.Name));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read the cover from {Path}", path);
            return null;
        }
    }

    public async Task UpdateCovers(MonitoredSeries series, IList<MonitoredChapter> chapters, string coverUrl,
        CancellationToken cancellationToken = default)
    {
        if (series.Format != Format.Archive)
        {
            throw new BadRequestException("Covers can only be updated for archive files");
        }

        if (chapters.Count == 0)
        {
            throw new BadRequestException("No chapters selected");
        }

        var volume = chapters[0].Volume.NormalizeVolumeMarker();
        if (chapters.Any(c => c.Volume.NormalizeVolumeMarker() != volume))
        {
            throw new BadRequestException("Select chapters of a single volume to update covers in bulk");
        }

        var covers = await GetCovers(series, cancellationToken);
        var cover = covers.FirstOrDefault(c => string.Equals(c.Url, coverUrl, StringComparison.Ordinal));
        if (cover == null)
        {
            throw new BadRequestException("The selected cover does not belong to this series");
        }

        var extension = GetCoverExtension(cover);
        var data = await DownloadCover(cover, series.Provider, cancellationToken);

        var archives = new List<(MonitoredChapter Chapter, string Path)>();
        foreach (var chapter in chapters)
        {
            var path = ResolveArchivePath(chapter);
            if (path == null)
            {
                throw new BadRequestException($"No archive found for chapter {chapter.Title}");
            }

            archives.Add((chapter, path));
        }

        foreach (var (chapter, path) in archives)
        {
            await WriteCover(path, data, extension, cancellationToken);

            chapter.LastModifiedUtc = DateTime.UtcNow;

            logger.LogInformation("Updated cover for {Chapter} ({Path})", chapter.Title, chapter.FilePath);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    private string? ResolveArchivePath(MonitoredChapter chapter)
    {
        if (string.IsNullOrEmpty(chapter.FilePath)) return null;

        chapter.FilePath.EnsureValidUserFilePath();

        var path = fileSystem.Path.Join(configuration.BaseDir, chapter.FilePath);
        if (!fileSystem.File.Exists(path)) return null;

        var extension = fileSystem.Path.GetExtension(path);
        if (!extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return path;
    }

    private ZipArchiveEntry? FindCoverEntry(ZipArchive archive)
    {
        var mnemaCover = archive.Entries.FirstOrDefault(e =>
            e.Name.StartsWith(CoverPrefix, StringComparison.OrdinalIgnoreCase));
        if (mnemaCover != null) return mnemaCover;

        return archive.Entries.FirstOrDefault(e =>
            !parserService.HasBlacklistedFolderInPath(e.FullName) && parserService.IsCoverImage(e.Name));
    }

    private async Task WriteCover(string path, byte[] data, string extension, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = fileSystem.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            await using var archive = new ZipArchive(stream, ZipArchiveMode.Update);

            FindCoverEntry(archive)?.Delete();

            var entry = archive.CreateEntry($"{CoverPrefix}{extension}", CompressionLevel.Optimal);
            await using var entryStream = await entry.OpenAsync(cancellationToken);
            await entryStream.WriteAsync(data, cancellationToken);
        }
        catch (IOException ex)
        {
            throw new BadRequestException($"Could not write the cover, {fileSystem.Path.GetFileName(path)} is in use", ex);
        }
    }

    private async Task<byte[]> DownloadCover(Cover cover, Provider provider, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(cover.Url, UriKind.Absolute, out _))
        {
            throw new BadRequestException("The selected cover has an invalid URL");
        }

        var client = httpClientFactory.CreateClient(provider.ToString());

        using var response =
            await client.GetAsync(cover.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new BadRequestException($"Failed to download the cover: {response.StatusCode}");
        }

        if (response.Content.Headers.ContentLength > MaxCoverSize)
        {
            throw new BadRequestException("The selected cover is too large");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType != null && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The selected cover is not an image");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);

        if (memory.Length > MaxCoverSize)
        {
            throw new BadRequestException("The selected cover is too large");
        }

        return memory.ToArray();
    }

    private string GetCoverExtension(Cover cover)
    {
        var extension = cover.Extension?.Trim();
        if (!string.IsNullOrEmpty(extension))
        {
            if (!extension.StartsWith('.')) extension = "." + extension;
        }
        else
        {
            extension = fileSystem.Path.GetExtension(cover.Url);
        }

        if (string.IsNullOrEmpty(extension) || !parserService.IsImage($"cover{extension}"))
        {
            throw new BadRequestException("The selected cover is not a supported image");
        }

        return extension.ToLowerInvariant();
    }

    private static float? ParseVolume(string? volume)
    {
        if (string.IsNullOrWhiteSpace(volume)) return null;

        return float.TryParse(volume, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
