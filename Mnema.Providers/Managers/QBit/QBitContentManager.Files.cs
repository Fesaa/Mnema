using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager
{
    public async Task<DownloadFilesDto> GetFiles(string contentId, CancellationToken ct = default)
    {
        var externalDownload = await GetExternalDownload(contentId, ct);

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var parserService = scope.ServiceProvider.GetRequiredService<IParserService>();

        List<MonitoredChapterOptionDto>? monitoredChapters = null;

        if (externalDownload.GetKey(RequestConstants.MonitoredSeriesId) is { } monitoredSeriesId)
        {
            var monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(monitoredSeriesId,
                MonitoredSeriesIncludes.Chapters, ct);

            if (monitoredSeries is null)
            {
                throw new BadRequestException($"Monitored series {monitoredSeriesId} no longer exists");
            }

            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
            monitoredChapters = mapper.Map<List<MonitoredChapterOptionDto>>(monitoredSeries.Chapters
                .Where(c => c.Status is MonitoredChapterStatus.Missing or MonitoredChapterStatus.Upcoming)
                .OrderBy(c => c.SortOrder)
                .ToList());
        }

        return new DownloadFilesDto
        {
            Files = externalDownload.Files.Select(file => new ExternalDownloadFileDto
            {
                Path = file.FullPath,
                FileName = file.FileName,
                FileSize = file.FileSize,
                Selected = file.Selected,
                VolumeMarker = parserService.EmptyIfLooseLeafVolume(file.VolumeMarker),
                ChapterMarker = parserService.EmptyIfDefaultChapter(file.ChapterMarker),
                MonitoredChapterId = file.MonitoredChapterId,
            }).ToList(),
            MonitoredChapters = monitoredChapters,
        };
    }

    public async Task UpdateFileMarkers(string contentId, IReadOnlyList<UpdateDownloadFileMarkersDto> files,
        CancellationToken ct = default)
    {
        var externalDownload = await GetExternalDownload(contentId, ct);

        if (externalDownload.State != ContentState.Waiting)
        {
            throw new BadRequestException("Markers can only be changed while the download is waiting");
        }

        var duplicatePaths = files
            .GroupBy(f => f.Path)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicatePaths.Count > 0)
        {
            throw new BadRequestException($"Duplicate file paths: {string.Join(", ", duplicatePaths)}");
        }

        var downloadFiles = externalDownload.Files.ToDictionary(f => f.FullPath);
        var unknownPaths = files
            .Where(f => !downloadFiles.ContainsKey(f.Path))
            .Select(f => f.Path)
            .ToList();
        if (unknownPaths.Count > 0)
        {
            throw new BadRequestException($"Unknown file paths: {string.Join(", ", unknownPaths)}");
        }

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        MonitoredSeries? monitoredSeries = null;
        if (externalDownload.GetKey(RequestConstants.MonitoredSeriesId) is { } monitoredSeriesId)
        {
            monitoredSeries = await unitOfWork.MonitoredSeriesRepository.GetById(monitoredSeriesId,
                MonitoredSeriesIncludes.Chapters, ct);

            if (monitoredSeries is null)
            {
                throw new BadRequestException($"Monitored series {monitoredSeriesId} no longer exists");
            }
        }

        var planned = new List<(DownloadFile File, string Volume, string Chapter, Guid? ChapterId)>();
        foreach (var update in files)
        {
            var file = downloadFiles[update.Path];

            if (monitoredSeries is null)
            {
                planned.Add((file, NormalizeMarker(update.VolumeMarker), NormalizeMarker(update.ChapterMarker), null));
                continue;
            }

            if (update.MonitoredChapterId is not { } chapterId)
                continue;

            var chapter = monitoredSeries.Chapters.FirstOrDefault(c => c.Id == chapterId)
                ?? throw new BadRequestException($"Chapter {chapterId} is not part of the monitored series");

            if (chapter.Status is not (MonitoredChapterStatus.Missing or MonitoredChapterStatus.Upcoming))
            {
                throw new BadRequestException($"Chapter {chapterId} is not available for assignment");
            }

            planned.Add((file, chapter.Volume, chapter.Chapter, chapter.Id));
        }

        foreach (var (file, volume, chapterMarker, chapterId) in planned)
        {
            file.VolumeMarker = volume;
            file.ChapterMarker = chapterMarker;
            file.MonitoredChapterId = chapterId;
        }

        unitOfWork.ExternalDownloadRepository.Update(externalDownload);
        await unitOfWork.CommitAsync(ct);
    }

    private static string NormalizeMarker(string? marker) => marker?.Trim() ?? string.Empty;
}
