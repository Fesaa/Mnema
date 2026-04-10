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
    IServiceProvider serviceProvider,
    IMessageService messageService,
    IConnectionService connectionService
): IMonitoredSeriesService
{
    public async Task UpdateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto,
        CancellationToken cancellationToken = default)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(dto.Id, MonitoredSeriesIncludes.Chapters, cancellationToken);
        if (series == null) throw new NotFoundException();

        if (series.UserId != userId) throw new ForbiddenException();

        if (await unitOfWork.MonitoredSeriesRepository.CheckDuplicateSeries(userId, series.Id, dto, cancellationToken))
        {
            throw new MnemaException("You cannot monitor the same series twice (External Ids or Valid Titles)");
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

    public async Task CreateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await unitOfWork.MonitoredSeriesRepository.CheckDuplicateSeries(userId, null, dto, cancellationToken))
        {
            throw new MnemaException("You cannot monitor the same series twice (External Ids or Valid Titles)");
        }

        var series = new MonitoredSeries
        {
            UserId = userId,
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

        var jobId = BackgroundJob.Enqueue(() => EnrichWithMetadata(series.Id, CancellationToken.None, true));
        if (!string.IsNullOrEmpty(jobId))
            jobId = BackgroundJob.ContinueJobWith(jobId, () => connectionService.CommunicateSeriesMonitored(series.Id, CancellationToken.None));

        if (string.IsNullOrEmpty(series.ExternalId)) return;

        if (string.IsNullOrEmpty(jobId))
        {
            BackgroundJob.Enqueue(() => StartDownload(userId, series.Id, true, CancellationToken.None));
        }
        else
        {
            BackgroundJob.ContinueJobWith(jobId, () => StartDownload(userId, series.Id, true, CancellationToken.None));
        }
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task StartDownload(Guid userId, Guid seriesId, bool firstDownload, CancellationToken ct = default)
    {
        var series = await unitOfWork.MonitoredSeriesRepository.GetById(seriesId, ct: ct);
        if (series == null) throw new NotFoundException();

        if (string.IsNullOrEmpty(series.ExternalId)) throw new MnemaException("Series has no external id");

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
            UserId = userId,
        });
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
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.TitleOverride.Key,
                    Field = "titleOverride",
                    Type = FormType.Text,
                },
                new FormControlDefinition
                {
                    Key = "valid-titles",
                    Field = "validTitles",
                    Type = FormType.MultiText,
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
                    Key = "provider",
                    Field = "provider",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = IMonitoredSeriesService.SupportedProviders
                        .Select(provider => new FormControlOption(provider.ToString().ToLower(), provider))
                        .ToList(),
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.FormatKey.Key,
                    Field = "format",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = Enum.GetValues<Format>()
                        .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                        .ToList(),
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.ContentFormatKey.Key,
                    Field = "contentFormat",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = Enum.GetValues<ContentFormat>()
                        .Select(f => new FormControlOption(f.ToString().ToLower(), f))
                        .ToList(),
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.HardcoverSeriesIdKey.Key,
                    Field = "hardcoverId",
                    Type = FormType.Text,
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.MangaBakaKey.Key,
                    Field = "mangaBakaId",
                    Type = FormType.Text,
                },
                new FormControlDefinition
                {
                    Key = RequestConstants.ExternalIdKey.Key,
                    Field = "externalId",
                    Type = FormType.Text,
                },
            ]
        };
    }

    public async Task<FormDefinition> GetMetadataForm(Guid userId, Provider provider, CancellationToken ct = default)
    {
        var excludedKeys = GetForm().Controls.Select(c => c.Key).ToHashSet();

        var allControls = new List<FormControlDefinition>();

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

    [AutomaticRetry(Attempts = 1)]
    public async Task EnrichWithMetadata(Guid guid, CancellationToken ct = default, bool firstRun = false)
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
            var pref = await unitOfWork.UserRepository.GetPreferences(mSeries.UserId);

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

        var seriesChapters = mSeries.Chapters;

        mSeries.Chapters = [];

        var allIds = series.Chapters.Select(c => c.Id).ToHashSet();
        unitOfWork.MonitoredSeriesRepository.RemoveRange(seriesChapters.Where(c => !allIds.Contains(c.ExternalId)));

        foreach (var chapter in series.Chapters)
        {
            var mChapter = seriesChapters.FirstOrDefault(c => c.ExternalId == chapter.Id);

            if (mChapter?.Status == MonitoredChapterStatus.NotMonitored)
            {
                PatchChapterMetadata(mChapter, chapter);
                mSeries.Chapters.Add(mChapter);
                continue;
            }

            var matchingFile = scannerService.FindMatch(onDiskContent, chapter);

            var status = MonitoredChapterStatus.Missing;
            if (matchingFile != null)
            {
                status = MonitoredChapterStatus.Available;
            }
            else if (chapter.ReleaseDate?.Date > DateTime.UtcNow.Date)
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

        await unitOfWork.CommitAsync(ct);

        await messageService.MetadataRefreshed(mSeries.UserId, mSeries.Id);
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
