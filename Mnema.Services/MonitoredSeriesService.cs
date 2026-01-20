using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;

namespace Mnema.Services;

public class MonitoredSeriesService(
    ILogger<MonitoredSeriesService> logger,
    IScannerService scannerService,
    IParserService parserService,
    IDownloadService downloadService,
    IMetadataResolver metadataResolver,
    ApplicationConfiguration configuration,
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider
): IMonitoredSeriesService
{
    public async Task UpdateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto,
        CancellationToken cancellationToken = default)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeries(dto.Id, cancellationToken);
        if (series == null) throw new NotFoundException();

        if (series.UserId != userId) throw new ForbiddenException();

        series.Title = dto.Title;
        series.BaseDir = dto.BaseDir;
        series.Providers = dto.Providers;
        series.ContentFormat = dto.ContentFormat;
        series.Format = dto.Format;
        series.ValidTitles = dto.ValidTitles;
        series.Metadata = dto.Metadata;

        await EnrichWithMetadata(series);

        unitOfWork.MonitoredSeriesRepository.Update(series);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task CreateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto,
        CancellationToken cancellationToken = default)
    {
        var series = new MonitoredSeries
        {
            UserId = userId,
            Title = dto.Title,
            BaseDir = dto.BaseDir,
            Providers = dto.Providers,
            ContentFormat = dto.Metadata.GetEnum<ContentFormat>(RequestConstants.FormatKey) ?? throw new MnemaException("A content format must be provided"),
            Format = dto.Metadata.GetEnum<Format>(RequestConstants.FormatKey) ?? throw new MnemaException("A format must be provided"),
            ValidTitles = dto.ValidTitles,
            Metadata = dto.Metadata,
            Chapters = [],
        };

        await EnrichWithMetadata(series, cancellationToken);

        unitOfWork.MonitoredSeriesRepository.Add(series);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public FormDefinition GetForm()
    {
        return new FormDefinition
        {
            Key = "edit-monitored-series-modal",
            Controls =
            [
                new FormControlDefinition
                {
                    Key = "title",
                    Field = "title",
                    Type = FormType.Text,
                    ForceSingle = true,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "valid-titles",
                    Field = "validTitles",
                    Type = FormType.MultiText,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMinLength(1)
                        .Build(),
                    ForceSingle = true,
                },
                new FormControlDefinition
                {
                    Key = "base-dir",
                    Field = "baseDir",
                    Type = FormType.Directory,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "providers",
                    Field = "providers",
                    Type = FormType.MultiSelect,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = IMonitoredSeriesService.SupportedProviders
                        .Select(provider => new FormControlOption(provider.ToString().ToLower(), provider))
                        .ToList(),
                },
            ]
        };
    }

    public async Task EnrichWithMetadata(MonitoredSeries mSeries, CancellationToken ct = default)
    {
        var series = await metadataResolver.ResolveSeriesAsync(mSeries.Metadata, ct);
        if (series == null)
        {
            logger.LogWarning("Monitored series {Title} has no metadata linked. Nothing will be downloaded", mSeries.Title);
            return;
        }

        var title = mSeries.Metadata.GetStringOrDefault(RequestConstants.TitleOverride, series.Title);
        if (string.IsNullOrEmpty(title))
            return;

        mSeries.CoverUrl = series.CoverUrl;
        mSeries.RefUrl = series.RefUrl;
        mSeries.Summary = series.Summary;

        var path = Path.Join(mSeries.BaseDir, title);
        var onDiskContent = scannerService.ScanDirectory(path, mSeries.ContentFormat, mSeries.Format, ct);

        var seriesChapters = mSeries.Chapters;
        mSeries.Chapters = [];

        foreach (var chapter in series.Chapters)
        {
            var mChapter = seriesChapters.FirstOrDefault(c => c.ExternalId == chapter.Id);

            if (mChapter?.Status == MonitoredChapterStatus.NotMonitored)
            {
                PatchChapterMetadata(mChapter, chapter);
                mSeries.Chapters.Add(mChapter);
                continue;
            }

            var matchingFile = FindMatchingFile(onDiskContent, chapter);

            var status = MonitoredChapterStatus.Missing;
            if (matchingFile != null)
            {
                status = MonitoredChapterStatus.Available;
            }
            else if (chapter.ReleaseDate >= DateTime.UtcNow)
            {
                status = MonitoredChapterStatus.Upcoming;
            }

            mChapter ??= new MonitoredChapter();
            PatchChapterMetadata(mChapter, chapter);
            mChapter.FilePath = matchingFile?.Path;
            mChapter.Status = status;

            mSeries.Chapters.Add(mChapter);
        }

        mSeries.LastDataRefreshUtc = DateTime.UtcNow;
    }

    private static void PatchChapterMetadata(MonitoredChapter? mChapter, Chapter chapter)
    {
        if (mChapter == null)
            return;

        mChapter.ExternalId = chapter.Id;
        mChapter.Title = chapter.Title;
        mChapter.Summary = chapter.Summary;
        mChapter.Volume = chapter.VolumeMarker;
        mChapter.Chapter = chapter.ChapterMarker;
        mChapter.CoverUrl = chapter.CoverUrl;
        mChapter.RefUrl = chapter.RefUrl;
        mChapter.ReleaseDate = chapter.ReleaseDate;
    }

    private static OnDiskContent? FindMatchingFile(List<OnDiskContent> onDiskContents, Chapter chapter)
    {
        if (string.IsNullOrEmpty(chapter.VolumeMarker) && string.IsNullOrEmpty(chapter.ChapterMarker))
        {
            return null;
        }

        if (string.IsNullOrEmpty(chapter.ChapterMarker))
        {
            return onDiskContents.FirstOrDefault(c
                => string.IsNullOrEmpty(c.Chapter) && c.Volume == chapter.VolumeMarker);
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
