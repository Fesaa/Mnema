using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
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
    IServiceProvider serviceProvider,
    IMessageService messageService,
    IConnectionService connectionService
): IMonitoredSeriesService
{
    public async Task UpdateMonitoredSeries(CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(dto.Id, MonitoredSeriesIncludes.Chapters, cancellationToken);
        if (series == null) throw new NotFoundException();

        if (await unitOfWork.MonitoredSeriesRepository.CheckDuplicateSeries(series.Id, dto, cancellationToken))
        {
            throw new BadRequestException("You cannot monitor the same series twice (External Ids or Valid Titles)");
        }

        series.Title = dto.Title;
        series.BaseDir = dto.BaseDir;
        series.Provider = dto.Provider;
        series.ContentFormat = dto.ContentFormat;
        series.Format = dto.Format;
        series.ValidTitles = dto.ValidTitles;
        series.TitleOverride = dto.TitleOverride;
        series.HardcoverId = dto.HardcoverId;
        series.MangaBakaId = dto.MangaBakaId;
        series.ExternalId = dto.ExternalId;
        series.Metadata = dto.Metadata;

        unitOfWork.MonitoredSeriesRepository.Update(series);

        await unitOfWork.CommitAsync(cancellationToken);

        BackgroundJob.Enqueue(() => EnrichWithMetadata(series.Id, CancellationToken.None));
    }

    public async Task<Guid> CreateMonitoredSeries(CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.MonitoredSeriesRepository.CheckDuplicateSeries(null, dto, cancellationToken))
        {
            throw new BadRequestException("You cannot monitor the same series twice (External Ids or Valid Titles)");
        }

        var series = new MonitoredSeries
        {
            Title = dto.Title,
            BaseDir = dto.BaseDir,
            Provider = dto.Provider,
            ContentFormat = dto.ContentFormat,
            Format = dto.Format,
            HardcoverId = dto.HardcoverId,
            MangaBakaId = dto.MangaBakaId,
            ExternalId = dto.ExternalId,
            Metadata = dto.Metadata,
            TitleOverride = dto.TitleOverride,
            ValidTitles = dto.ValidTitles,
            Summary = string.Empty,
            Chapters = [],
        };

        unitOfWork.MonitoredSeriesRepository.Add(series);

        await unitOfWork.CommitAsync(cancellationToken);

        var jobId = BackgroundJob.Enqueue(() => EnrichWithMetadata(series.Id, true, CancellationToken.None));
        if (!string.IsNullOrEmpty(jobId))
            jobId = BackgroundJob.ContinueJobWith(jobId, () => connectionService.CommunicateSeriesMonitored(series.Id, CancellationToken.None));

        if (string.IsNullOrEmpty(series.ExternalId)) return series.Id;

        if (string.IsNullOrEmpty(jobId))
        {
            BackgroundJob.Enqueue(() => StartDownload(series.Id, true, CancellationToken.None));
        }
        else
        {
            BackgroundJob.ContinueJobWith(jobId, () => StartDownload(series.Id, true, CancellationToken.None));
        }

        return series.Id;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task StartDownload(Guid seriesId, bool firstDownload, CancellationToken ct = default)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(seriesId, ct: ct);
        if (series == null) throw new NotFoundException();

        if (string.IsNullOrEmpty(series.ExternalId)) throw new BadRequestException("Series has no external id");

        var metadata = series.MetadataForDownloadRequest();
        metadata.SetKey(RequestConstants.FirstDownload, firstDownload);

        await downloadService.StartDownload(new DownloadRequestDto
        {
            Provider = series.Provider,
            Id = series.ExternalId,
            BaseDir = series.BaseDir,
            TempTitle = series.Title,
            Metadata = metadata,
            StartImmediately = true,
        });
    }

    public FormDefinition GetForm()
    {
        return new FormDefinition
        {
            Key = "edit-monitored-series-modal",
            Controls =
            [
                new TextFieldDefinition
                {
                    Key = "title",
                    Field = "title",
                    Validators = FormValidatorsBuilder.Required,
                },
                new TextFieldDefinition
                {
                    Key = RequestConstants.TitleOverride.Key,
                    Field = "titleOverride",
                },
                new MultiTextFieldDefinition
                {
                    Key = "valid-titles",
                    Field = "validTitles",
                    ForceSingle = true,
                },
                new DirectoryFieldDefinition
                {
                    Key = "base-dir",
                    Field = "baseDir",
                    Validators = FormValidatorsBuilder.Required,
                },
                FormFieldDefinitions.EnumDropDown("provider", "provider-name-pipe", IMonitoredSeriesService.SupportedProviders, false),
                FormFieldDefinitions.EnumDropDown<Format>("format", "format-pipe", false),
                FormFieldDefinitions.EnumDropDown<ContentFormat>("contentFormat", "content-format-pipe", false),
                new TextFieldDefinition
                {
                    Key = RequestConstants.HardcoverSeriesIdKey.Key,
                    Field = "hardcoverId",
                },
                new TextFieldDefinition
                {
                    Key = RequestConstants.MangaBakaKey.Key,
                    Field = "mangaBakaId",
                },
                new TextFieldDefinition
                {
                    Key = RequestConstants.ExternalIdKey.Key,
                    Field = "externalId",
                },
            ]
        };
    }

    public async Task<FormDefinition> GetMetadataForm(Provider provider, CancellationToken ct = default)
    {
        var excludedKeys = GetForm().Controls.Select(c => c.Key).ToHashSet();

        var allControls = new List<FormFieldDefinition>();

        var repository = serviceProvider.GetKeyedService<IContentRepository>(provider);
        if (repository != null)
        {
            var controls = await repository.DownloadMetadata(ct);
            allControls.AddRange(controls.Where(c => !excludedKeys.Contains(c.Key)).ToList());
        }

        return new FormDefinition
        {
            Key = "provider-metadata",
            Controls = allControls
        };
    }

    public async Task EnrichWithMetadata(Guid guid, CancellationToken ct = default)
    {
        await EnrichWithMetadata(guid, false, ct);
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task EnrichWithMetadata(Guid guid, bool firstRun = false, CancellationToken ct = default)
    {
        var mSeries = await unitOfWork.MonitoredSeriesRepository.GetById(guid, MonitoredSeriesIncludes.Chapters, ct);
        if (mSeries == null) return;

        var metadata = mSeries.MetadataForDownloadRequest();

        var series = await metadataResolver.ResolveSeriesAsync(mSeries.Provider, metadata, ct);
        if (series == null)
        {
            logger.LogWarning("Monitored series {Title} has no metadata linked. Nothing will be downloaded", mSeries.Title);
            return;
        }

        if (firstRun)
        {
            var pref = await unitOfWork.SettingsRepository.GetPreferencesAsync(ct);

            if (pref.PinSubscriptionTitles && string.IsNullOrEmpty(mSeries.TitleOverride) && !string.IsNullOrEmpty(series.Title))
            {
                mSeries.TitleOverride = series.Title;
                unitOfWork.MonitoredSeriesRepository.Update(mSeries);
                await unitOfWork.CommitAsync(ct);

                metadata = mSeries.MetadataForDownloadRequest();
            }
        }

        var title = metadata.GetKey(RequestConstants.TitleOverride) ?? series.Title;
        if (string.IsNullOrEmpty(title))
        {
            logger.LogWarning("Resolved series {Title} has no title. not using as metadata", mSeries.Title);
            return;
        }

        if (!string.IsNullOrEmpty(series.CoverUrl))
        {
            mSeries.CoverUrl = series.CoverUrl.StartsWith("proxy") ? $"api/{series.CoverUrl}" : series.CoverUrl;
        }

        mSeries.RefUrl = series.RefUrl;
        mSeries.Summary = series.Summary;

        var path = Path.Join(mSeries.BaseDir, title);
        var onDiskContent = scannerService.ScanDirectory(path, mSeries.ContentFormat, mSeries.Format, ct);

        SyncChapters(mSeries, series.Chapters, onDiskContent);

        if (series.ContentFormat is not null)
        {
            switch (series.ContentFormat)
            {
                case ContentFormat.Comic:
                case ContentFormat.Manga:
                    mSeries.ContentFormat = ContentFormat.Manga;
                    mSeries.Format = Format.Archive;
                    break;
                case ContentFormat.LightNovel:
                case ContentFormat.Book:
                    mSeries.ContentFormat = ContentFormat.Book;
                    mSeries.Format = Format.Epub;
                break;
            }
        }

        mSeries.LastDataRefreshUtc = DateTime.UtcNow;

        await unitOfWork.CommitAsync(ct);

        await messageService.MetadataRefreshed(mSeries.Id);
    }

    private void SyncChapters(MonitoredSeries mSeries, IList<Chapter> upstreamChapters, List<OnDiskContent> onDiskContent)
    {
        var existingChapters = mSeries.Chapters;
        mSeries.Chapters = [];

        var upstreamIds = upstreamChapters.Select(c => c.Id).ToHashSet();
        var removedChapters = existingChapters.Where(c => !upstreamIds.Contains(c.ExternalId));
        unitOfWork.MonitoredSeriesRepository.RemoveRange(removedChapters);

        foreach (var upstreamChapter in upstreamChapters)
        {
            var existingChapter = existingChapters.FirstOrDefault(c => c.ExternalId == upstreamChapter.Id);
            mSeries.Chapters.Add(SyncChapter(existingChapter, upstreamChapter, onDiskContent));
        }

        mSeries.UnMatchedChapters.Clear();
        mSeries.UnMatchedChapters.AddRange(onDiskContent.Select(file => new RawFile
        {
            Path = file.Path.RemovePrefix(configuration.BaseDir),
            Chapter = file.ChapterMarker,
            Volume = file.VolumeMarker,
            ComicInfo = file.ComicInfo
        }));
    }

    private MonitoredChapter SyncChapter(MonitoredChapter? existingChapter, Chapter upstreamChapter, List<OnDiskContent> onDiskContent)
    {
        if (existingChapter?.Status == MonitoredChapterStatus.NotMonitored)
        {
            PatchChapterMetadata(existingChapter, upstreamChapter);
            return existingChapter;
        }

        var matchingFile = parserService.FindMatch(onDiskContent, upstreamChapter);
        if (matchingFile != null)
        {
            onDiskContent.Remove(matchingFile);
        }

        var mChapter = existingChapter ?? new MonitoredChapter();
        PatchChapterMetadata(mChapter, upstreamChapter);
        mChapter.FilePath = matchingFile?.Path.RemovePrefix(configuration.BaseDir);
        mChapter.ComicInfo = matchingFile?.ComicInfo;
        mChapter.Status = DetermineStatus(matchingFile, upstreamChapter);

        return mChapter;
    }

    private static MonitoredChapterStatus DetermineStatus(OnDiskContent? matchingFile, Chapter upstreamChapter)
    {
        if (matchingFile != null)
            return MonitoredChapterStatus.Available;

        if (upstreamChapter.ReleaseDate?.Date > DateTime.UtcNow.Date)
            return MonitoredChapterStatus.Upcoming;

        return MonitoredChapterStatus.Missing;
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
        mChapter.ReleaseDate = chapter.ReleaseDate?.ToUniversalTime();
        mChapter.SortOrder = chapter.SortOrder ?? ParserService.SpecialVolumeNumber;
    }


}
