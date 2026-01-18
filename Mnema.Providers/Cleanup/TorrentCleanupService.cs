using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.External;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Providers.QBit;

namespace Mnema.Providers.Cleanup;

internal class TorrentCleanupService(
    ILogger<TorrentCleanupService> logger,
    INamingService namingService,
    IScannerService scannerService,
    IParserService parserService,
    IFileSystem fileSystem,
    IImageService imageService,
    [FromKeyedServices(key: MetadataProvider.Hardcover)] IMetadataProviderService hardcoverMetadataProvider,
    IMetadataService metadataService,
    ApplicationConfiguration configuration,
    IUnitOfWork unitOfWork
    ): ICleanupService
{
    private static readonly XmlSerializer ComicInfoSerializer = new(typeof(ComicInfo));
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"];

    public async Task Cleanup(IContent content)
    {
        if (content is not QBitTorrent torrent)
            throw new MnemaException($"{nameof(PublicationCleanupService)} cannot cleanup {content.GetType()}");

        logger.LogDebug("[{Title}/{Id}] Cleaning up torrent", content.Title, content.Id);

        var request = content.Request;
        var series = await GetMetadata(request);
        var preferences = await unitOfWork.UserRepository.GetPreferences(request.UserId);

        var downloadDir = content.DownloadDir;

        var format = request.Metadata.GetEnum<Format>(RequestConstants.FormatKey) ??  Format.Archive;
        var contentFormat = request.Metadata.GetEnum<ContentFormat>(RequestConstants.ContentFormatKey) ?? ContentFormat.Manga;

        var title = request.Metadata.GetString(RequestConstants.TitleOverride)
            .OrNonEmpty(series?.Title,
                parserService.ParseSeries(torrent.Title, contentFormat),
                request.TempTitle);

        var destDir = fileSystem.Path.Join(configuration.BaseDir, request.BaseDir, title);

        if (!fileSystem.Directory.Exists(destDir))
            fileSystem.Directory.CreateDirectory(destDir);

        var files = fileSystem.Directory.GetFiles(downloadDir, "*", SearchOption.AllDirectories);
        var allowedExtensions = parserService.FileExtensionsForFormat(format);

        foreach (var sourceFile in files)
        {
            var ext = fileSystem.Path.GetExtension(sourceFile).ToLower();

            if (!allowedExtensions.IsMatch(ext))
                continue;

            var fileName = fileSystem.Path.GetFileName(sourceFile);
            var volume = parserService.ParseVolume(fileName, contentFormat);
            var chapter = parserService.ParseChapter(fileName, contentFormat);
            volume = parserService.IsLooseLeafVolume(volume) ? null : volume;
            chapter = parserService.IsDefaultChapter(chapter) ? string.Empty : chapter;

            var chapterFileName = namingService.GetChapterFileName(
                title,
                volume,
                chapter,
                chapter.AsFloat(),
                string.IsNullOrEmpty(chapter) && string.IsNullOrEmpty(volume),
                null,
                []
            );

            var destPath = fileSystem.Path.Join(destDir, chapterFileName + ".cbz");
            var chapterEntity = FindChapter(series, volume, chapter);
            var ci = metadataService.CreateComicInfo(preferences, request, title, series, chapterEntity);

            switch (format)
            {
                case Format.Archive:
                    await CleanupArchive(sourceFile, destPath, ci, preferences);
                    break;
                case Format.Epub:
                    break;
                case Format.Unsupported:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }

    private async Task CleanupArchive(
        string sourceFile,
        string destinationPath,
        ComicInfo? ci,
        UserPreferences preferences)
    {
        var tempDirName = fileSystem.Path.GetFileNameWithoutExtension(destinationPath);
        var tempDirPath = fileSystem.Path.Join(fileSystem.Path.GetTempPath(), "Mnema", tempDirName);
        var extractPath = fileSystem.Path.Join(tempDirPath, "extract");
        var finalPath = fileSystem.Path.Join(tempDirPath, "final");

        if (fileSystem.Directory.Exists(tempDirPath))
            fileSystem.Directory.Delete(tempDirPath, true);

        fileSystem.Directory.CreateDirectory(tempDirPath);
        fileSystem.Directory.CreateDirectory(extractPath);
        fileSystem.Directory.CreateDirectory(finalPath);

        try
        {
            await ZipFile.ExtractToDirectoryAsync(sourceFile, extractPath);

            var sourceFiles = fileSystem.Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);

            foreach (var file in sourceFiles)
            {
                var fileName = fileSystem.Path.GetFileName(file);
                var ext = fileSystem.Path.GetExtension(file).ToLower();

                if (ImageExtensions.Contains(ext))
                {
                    var destExt = preferences.ImageFormat.GetFileExtension(fileName);
                    var destImageName = fileSystem.Path.GetFileNameWithoutExtension(fileName) + destExt;
                    var destImagePath = fileSystem.Path.Join(finalPath, destImageName);

                    await using var stream = fileSystem.File.OpenRead(file);
                    await imageService.ConvertAndSave(stream, preferences.ImageFormat, destImagePath);
                }
                else if (ci != null && fileName.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip existing ComicInfo.xml, we will provide a new one
                }
                else
                {
                    var destFilePath = fileSystem.Path.Join(finalPath, fileName);
                    fileSystem.File.Copy(file, destFilePath, true);
                }
            }

            if (ci != null)
            {
                var ciPath = fileSystem.Path.Join(finalPath, "ComicInfo.xml");
                await using var ciStream = fileSystem.File.OpenWrite(ciPath);
                await using var writer = new StreamWriter(ciStream);
                ComicInfoSerializer.Serialize(writer, ci);
            }

            if (fileSystem.File.Exists(destinationPath))
                fileSystem.File.Delete(destinationPath);

            await ZipFile.CreateFromDirectoryAsync(finalPath, destinationPath, CompressionLevel.SmallestSize, false);
        }
        finally
        {
            if (fileSystem.Directory.Exists(tempDirPath))
                fileSystem.Directory.Delete(tempDirPath, true);
        }
    }

    private static Chapter? FindChapter(Series? series, string? volume, string? chapter)
    {
        if (series == null)
            return null;

        if (string.IsNullOrEmpty(chapter) && string.IsNullOrEmpty(volume))
            return null;

        return series.Chapters.FirstOrDefault(c =>
        {
            if (string.IsNullOrEmpty(chapter) && MatchingIfNotNull(c.VolumeMarker, volume))
                return true;

            return MatchingIfNotNull(c.VolumeMarker, volume) && MatchingIfNotNull(chapter, c.ChapterMarker);
        });

        bool MatchingIfNotNull(string? first, string? second)
            => !string.IsNullOrEmpty(first)
            && !string.IsNullOrEmpty(second)
            && first == second;
    }

    private Task<Series?> GetMetadata(DownloadRequestDto request)
    {
        var hardCoverId = request.Metadata.GetString(RequestConstants.HardcoverSeriesIdKey);
        var mangaBakaId = request.Metadata.GetString(RequestConstants.MangaBakaKey);

        if (!string.IsNullOrEmpty(mangaBakaId))
            return Task.FromResult<Series?>(null);

        if (!string.IsNullOrEmpty(hardCoverId))
            return hardcoverMetadataProvider.GetSeries(hardCoverId, CancellationToken.None);

        return Task.FromResult<Series?>(null);
    }
}
