using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
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

namespace Mnema.Providers.Cleanup;

internal class RawFileCleanupService(
    ILogger<RawFileCleanupService> logger,
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
    private static readonly ParallelOptions ParallelOptions = new() { MaxDegreeOfParallelism = 2 };
    private readonly Dictionary<Format, IFormatHandler> _handlers = formatHandlers.ToDictionary(h => h.SupportedFormat);

    public async Task CleanupAsync(IContent content, CancellationToken cancellationToken = default)
    {

        var request = content.Request;
        var context = await BuildCleanupContextAsync(request, content);

        logger.LogDebug("[{Title}/{Id}] Cleaning up torrent - {Dir}", content.Title, content.Id, context.DownloadDirectory);

        await ProcessFilesAsync(context);
    }

    private async Task<CleanupContext> BuildCleanupContextAsync(DownloadRequestDto request, IContent content)
    {
        if (content == null) throw new ArgumentNullException(nameof(content));

        var preferences = await unitOfWork.UserRepository.GetPreferences(request.UserId);

        var series = await metadataResolver.ResolveSeriesAsync(request.Provider, request.Metadata);

        var format = request.Metadata.GetKey(RequestConstants.FormatKey);
        var contentFormat = request.Metadata.GetKey(RequestConstants.ContentFormatKey);

        var title = ResolveTitle(request, series, content, contentFormat);
        var destDir = PrepareDestinationDirectory(request, title);

        var downloadDir = content.DownloadDir;
        if (!downloadDir.StartsWith(configuration.DownloadDir))
        {
            downloadDir = Path.Join(configuration.DownloadDir, downloadDir);
        }

        return new CleanupContext(
            Request: request,
            Series: series,
            Preferences: preferences,
            Format: format,
            ContentFormat: contentFormat,
            Title: title,
            DestinationDirectory: destDir,
            DownloadDirectory: downloadDir
        );
    }

    private string ResolveTitle(DownloadRequestDto request, Series? series, IContent content, ContentFormat contentFormat)
    {
        return request.Metadata.GetKey(RequestConstants.TitleOverride)
            .OrNonEmpty(
                series?.Title,
                parserService.ParseSeries(content.Title, contentFormat),
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

        var validFiles = files.Where(Filter).ToList();
        if (validFiles.Count == 0)
        {
            logger.LogWarning("[{Title}/{Id}] No files found in directory {Directory} that match the format",
                context.Title, context.Series?.Id, context.DownloadDirectory);
            return;
        }

        await Parallel.ForEachAsync(validFiles, ParallelOptions,
            async (f, _) => await ProcessSingleFileAsync(context, f));
        return;

        bool Filter(string f)
        {
            var allowed = allowedExtensions.IsMatch(fileSystem.Path.GetExtension(f));
            if (!allowed)
            {
                logger.LogDebug("[{Title}/{Id}] Skipping file {FileName} as it does not match the format {Format}",
                    context.Title, context.Series?.Id, f, context.Format);
            }

            return allowed;
        }
    }

    private async Task ProcessSingleFileAsync(CleanupContext context, string sourceFile)
    {
        var ignoreNonMatched = context.Request.Metadata.GetKey(RequestConstants.IgnoreNonMatchedVolumes);

        logger.LogDebug("Processing file {FileName} for cleanup", sourceFile);

        var fileName = fileSystem.Path.GetFileName(sourceFile);
        var resolution = metadataResolver.ResolveChapter(fileName, context.Series, context.ContentFormat);
        if (resolution.ChapterEntity == null && ignoreNonMatched && context.Series?.Chapters.Count > 0)
        {
            logger.LogDebug("[{Title}/{Id}] Skipping file {FileName} as it could not be matched",
                context.Title, context.Series?.Id, fileName);
            return;
        }

        var chapterFileName = BuildChapterFileName(context.Title, resolution);
        var destPath = fileSystem.Path.Join(context.DestinationDirectory, chapterFileName + context.Format.FileExt());

        var comicInfo = metadataService.CreateComicInfo(
            context.Preferences,
            context.Request,
            context.Title,
            context.Series,
            resolution.ChapterEntity
        );

        if (string.IsNullOrEmpty(comicInfo?.Volume) && !string.IsNullOrEmpty(resolution.Volume))
            comicInfo?.Volume = resolution.Volume;

        if (string.IsNullOrEmpty(comicInfo?.Number) && !string.IsNullOrEmpty(resolution.Chapter))
            comicInfo?.Number = resolution.Chapter;

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
