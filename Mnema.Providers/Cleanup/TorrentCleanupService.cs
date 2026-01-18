using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
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
    IParserService parserService,
    IFileSystem fileSystem,
    IMetadataService metadataService,
    ApplicationConfiguration configuration,
    IUnitOfWork unitOfWork,
    IEnumerable<IFormatHandler> formatHandlers,
    IMetadataResolver metadataResolver
) : ICleanupService
{
    private readonly Dictionary<Format, IFormatHandler> _handlers = formatHandlers.ToDictionary(h => h.SupportedFormat);

    public async Task Cleanup(IContent content)
    {
        if (content is not QBitTorrent torrent)
            throw new MnemaException($"{nameof(PublicationCleanupService)} cannot cleanup {content.GetType()}");

        logger.LogDebug("[{Title}/{Id}] Cleaning up torrent", content.Title, content.Id);

        var request = content.Request;
        var context = await BuildCleanupContextAsync(request, torrent);

        await ProcessFilesAsync(context);
    }

    private async Task<CleanupContext> BuildCleanupContextAsync(DownloadRequestDto request, QBitTorrent torrent)
    {
        var series = await metadataResolver.ResolveSeriesAsync(request.Metadata);
        var preferences = await unitOfWork.UserRepository.GetPreferences(request.UserId);

        var format = request.Metadata.GetEnum<Format>(RequestConstants.FormatKey) ?? Format.Archive;
        var contentFormat = request.Metadata.GetEnum<ContentFormat>(RequestConstants.ContentFormatKey) ?? ContentFormat.Manga;

        var title = ResolveTitle(request, series, torrent, contentFormat);
        var destDir = PrepareDestinationDirectory(request, title);

        return new CleanupContext(
            Request: request,
            Series: series,
            Preferences: preferences,
            Format: format,
            ContentFormat: contentFormat,
            Title: title,
            DestinationDirectory: destDir,
            DownloadDirectory: torrent.DownloadDir
        );
    }

    private string ResolveTitle(DownloadRequestDto request, Series? series, QBitTorrent torrent, ContentFormat contentFormat)
    {
        return request.Metadata.GetString(RequestConstants.TitleOverride)
            .OrNonEmpty(
                series?.Title,
                parserService.ParseSeries(torrent.Title, contentFormat),
                request.TempTitle
            );
    }

    private string PrepareDestinationDirectory(DownloadRequestDto request, string title)
    {
        var destDir = fileSystem.Path.Join(configuration.BaseDir, request.BaseDir, title);

        if (!fileSystem.Directory.Exists(destDir))
            fileSystem.Directory.CreateDirectory(destDir);

        return destDir;
    }

    private async Task ProcessFilesAsync(CleanupContext context)
    {
        var files = fileSystem.Directory.GetFiles(context.DownloadDirectory, "*", SearchOption.AllDirectories);
        var allowedExtensions = parserService.FileExtensionsForFormat(context.Format);

        foreach (var sourceFile in files)
        {
            if (!allowedExtensions.IsMatch(fileSystem.Path.GetExtension(sourceFile)))
                continue;

            await ProcessSingleFileAsync(context, sourceFile);
        }
    }

    private async Task ProcessSingleFileAsync(CleanupContext context, string sourceFile)
    {
        logger.LogDebug("Processing file {FileName} for cleanup", sourceFile);

        var fileName = fileSystem.Path.GetFileName(sourceFile);
        var resolution = metadataResolver.ResolveChapter(fileName, context.Series, context.ContentFormat);

        var chapterFileName = BuildChapterFileName(context.Title, resolution);
        var destPath = fileSystem.Path.Join(context.DestinationDirectory, chapterFileName + ".cbz");

        var comicInfo = metadataService.CreateComicInfo(
            context.Preferences,
            context.Request,
            context.Title,
            context.Series,
            resolution.ChapterEntity
        );

        var coverUrl = resolution.ChapterEntity?.CoverUrl ?? context.Series?.CoverUrl;

        await HandleFormatAsync(context, sourceFile, destPath, coverUrl, comicInfo);
    }

    private string BuildChapterFileName(string title, ChapterResolutionResult resolution)
    {
        var isUnnumbered = string.IsNullOrEmpty(resolution.Chapter) && string.IsNullOrEmpty(resolution.Volume);

        return namingService.GetChapterFileName(
            title,
            resolution.Volume,
            resolution.Chapter ?? string.Empty,
            resolution.Chapter.AsFloat(),
            isUnnumbered,
            resolution.ChapterEntity?.Title,
            []
        );
    }

    private async Task HandleFormatAsync(
        CleanupContext context,
        string sourceFile,
        string destPath,
        string? coverUrl,
        ComicInfo? comicInfo)
    {
        if (!_handlers.TryGetValue(context.Format, out var handler))
        {
            logger.LogWarning("No handler found for format {Format}", context.Format);
            return;
        }

        var handlerContext = new FormatHandlerContext(
            sourceFile,
            destPath,
            coverUrl,
            comicInfo,
            context.Preferences,
            context.Request
        );

        await handler.HandleAsync(handlerContext);
    }
}


internal record CleanupContext(
    DownloadRequestDto Request,
    Series? Series,
    UserPreferences Preferences,
    Format Format,
    ContentFormat ContentFormat,
    string Title,
    string DestinationDirectory,
    string DownloadDirectory
);
