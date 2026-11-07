using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Internal;

namespace Mnema.Services;

public class ChapterCombineService(
    ILogger<ChapterCombineService> logger,
    IFileSystem fileSystem,
    IParserService parserService,
    IMetadataService metadataService,
    INamingService namingService,
    ApplicationConfiguration configuration
) : IChapterCombineService
{
    private const string ComicInfoFileName = "ComicInfo.xml";
    private const string CoverPrefix = "!0000 cover";

    public async Task<ChapterCombineResult> Combine(MonitoredSeries series, IReadOnlyList<MonitoredChapter> chapters,
        CancellationToken cancellationToken = default)
    {
        if (chapters.Count < 2) throw new BadRequestException("Select at least two chapters");

        if (series.Format != Format.Archive) throw new BadRequestException("Only archive chapters can be combined");

        var volume = chapters[0].Volume.NormalizeVolumeMarker();
        if (string.IsNullOrEmpty(volume)) throw new BadRequestException("Select chapters with a volume");

        if (chapters.Any(c => c.Volume.NormalizeVolumeMarker() != volume))
        {
            throw new BadRequestException("Select chapters of a single volume");
        }

        if (chapters.Any(c => string.IsNullOrWhiteSpace(c.Chapter)))
        {
            throw new BadRequestException("Select chapter files, not volume files");
        }

        if (chapters.Any(c => c.Status != MonitoredChapterStatus.Available))
        {
            throw new BadRequestException("All selected chapters must be available on disk");
        }

        var ordered = parserService.Sort(chapters).ToList();
        var paths = ordered.Select(ResolvePath).ToList();

        if (paths.Distinct(StringComparer.Ordinal).Count() != paths.Count)
        {
            throw new BadRequestException("Selection contains the same file more than once");
        }

        var first = ordered[0];
        var last = ordered[^1];
        var range = first.Chapter == last.Chapter ? first.Chapter : $"{first.Chapter}-{last.Chapter}";

        var outputDirectory = fileSystem.Path.GetDirectoryName(paths[0]);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            throw new BadRequestException($"Could not determine the directory for {paths[0]}");
        }

        var title = string.IsNullOrEmpty(series.TitleOverride) ? series.Title : series.TitleOverride;
        if (string.IsNullOrWhiteSpace(title)) throw new BadRequestException("Series has no title");

        var fileName = namingService.GetVolumeFileName(title, first.Volume);
        var outputPath = fileSystem.Path.Join(outputDirectory, fileName + ".cbz");
        if (fileSystem.File.Exists(outputPath)) throw new BadRequestException($"File already exists: {fileName}.cbz");

        var comicInfo = metadataService.ParseComicInfoFromFile(paths[0])
                        ?? first.ComicInfo
                        ?? new ComicInfo();

        if (string.IsNullOrEmpty(comicInfo.Series))
        {
            comicInfo.Series = title;
        }

        comicInfo.Volume = first.Volume;
        comicInfo.Number = range;

        var tempPath = outputPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var pageCount = await WriteArchive(paths, tempPath, cancellationToken);
            if (pageCount == 0)
            {
                throw new BadRequestException("The selected chapters contain no images");
            }

            await metadataService.WriteComicInfo(comicInfo, tempPath, cancellationToken);

            try
            {
                fileSystem.File.Move(tempPath, outputPath);
            }
            catch (IOException ex)
            {
                throw new BadRequestException($"Could not write {fileName}.cbz: {ex.Message}", ex);
            }
        }
        finally
        {
            if (fileSystem.File.Exists(tempPath))
            {
                fileSystem.File.Delete(tempPath);
            }
        }

        foreach (var path in paths)
        {
            try
            {
                fileSystem.File.Delete(path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete source chapter {Path}", path);
            }
        }

        logger.LogInformation("Combined {Count} chapters into {FilePath}", paths.Count, outputPath);

        return new ChapterCombineResult(outputPath.RemovePrefix(configuration.BaseDir), comicInfo);
    }

    private string ResolvePath(MonitoredChapter chapter)
    {
        if (string.IsNullOrEmpty(chapter.FilePath))
        {
            throw new BadRequestException($"Chapter has no file on disk: {chapter.Title}");
        }

        chapter.FilePath.EnsureValidUserFilePath();

        var path = fileSystem.Path.Join(configuration.BaseDir, chapter.FilePath);
        if (!fileSystem.File.Exists(path))
        {
            throw new BadRequestException($"File not found: {chapter.FilePath}");
        }

        if (parserService.ParseFormat(path) != Format.Archive)
        {
            throw new BadRequestException($"Not an archive: {chapter.FilePath}");
        }

        return path;
    }

    private async Task<int> WriteArchive(IReadOnlyList<string> paths, string tempPath,
        CancellationToken cancellationToken)
    {
        var pageIndex = 0;
        var wroteCover = false;

        await using var outputStream = fileSystem.File.Create(tempPath);
        using (var outputArchive = new ZipArchive(outputStream, ZipArchiveMode.Create))
        {
            foreach (var path in paths)
            {
                await using var inputStream = fileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var inputArchive = new ZipArchive(inputStream, ZipArchiveMode.Read);

                foreach (var entry in inputArchive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue;
                    if (entry.Name.Equals(ComicInfoFileName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (parserService.HasBlacklistedFolderInPath(entry.FullName)) continue;
                    if (!parserService.IsImage(entry.Name)) continue;

                    if (IsCoverEntry(entry))
                    {
                        if (wroteCover) continue;

                        wroteCover = true;
                        await CopyEntry(entry, outputArchive,
                            CoverPrefix + fileSystem.Path.GetExtension(entry.Name), cancellationToken);
                        continue;
                    }

                    pageIndex++;
                    await CopyEntry(entry, outputArchive,
                        $"page {pageIndex:0000}{fileSystem.Path.GetExtension(entry.Name)}", cancellationToken);
                }
            }
        }

        return pageIndex;
    }

    private bool IsCoverEntry(ZipArchiveEntry entry)
    {
        return entry.Name.StartsWith(CoverPrefix, StringComparison.OrdinalIgnoreCase)
               || parserService.IsCoverImage(entry.Name);
    }

    private static async Task CopyEntry(ZipArchiveEntry entry, ZipArchive outputArchive, string name,
        CancellationToken cancellationToken)
    {
        var outputEntry = outputArchive.CreateEntry(name, CompressionLevel.Optimal);
        await using var outputStream = outputEntry.Open();
        await using var inputStream = entry.Open();
        await inputStream.CopyToAsync(outputStream, cancellationToken);
    }
}
