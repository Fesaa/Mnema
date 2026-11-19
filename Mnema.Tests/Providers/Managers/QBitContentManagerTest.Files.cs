using System;
using System.Linq;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Services;

namespace Mnema.Tests.Providers.Managers;

public partial class QBitContentManagerTest
{
    private static ExternalDownload CreateExternalDownload(ContentState state, MetadataBag? metadata = null) => new()
    {
        Id = Guid.NewGuid(),
        ExternalId = SpiceAndWolfHash,
        Title = "Spice and Wolf",
        BaseDir = "Manga",
        Provider = Provider.Nyaa,
        State = state,
        Metadata = metadata ?? new MetadataBag(),
        Files =
        [
            new DownloadFile
            {
                FileName = "Spice and Wolf v01.cbz",
                FullPath = "Spice and Wolf v01.cbz",
                FileSize = 1024,
                VolumeMarker = "1",
                ChapterMarker = "1",
                Selected = true,
            },
            new DownloadFile
            {
                FileName = "Spice and Wolf v02.cbz",
                FullPath = "Spice and Wolf v02.cbz",
                FileSize = 2048,
                VolumeMarker = "2",
                ChapterMarker = "1",
                Selected = false,
            },
        ],
    };

    private static MonitoredSeries CreateMonitoredSeries() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Spice and Wolf",
        NormalizedTitle = "spice and wolf",
        Summary = string.Empty,
        BaseDir = "Manga",
        Provider = Provider.Nyaa,
        ContentFormat = ContentFormat.Manga,
        Format = Format.Archive,
        HardcoverId = string.Empty,
        MangaBakaId = string.Empty,
        ExternalId = string.Empty,
        TitleOverride = string.Empty,
        Metadata = new MetadataBag(),
        Chapters = [],
        UnMatchedChapters = [],
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow,
        LastDataRefreshUtc = DateTime.UtcNow,
    };

    private static MonitoredChapter CreateMonitoredChapter(MonitoredSeries series, string volume, string chapter,
        MonitoredChapterStatus status, float sortOrder, string title)
    {
        var id = Guid.NewGuid();
        return new MonitoredChapter
        {
            Id = id,
            ExternalId = id.ToString(),
            SeriesId = series.Id,
            Status = status,
            Title = title,
            Summary = string.Empty,
            Volume = volume,
            Chapter = chapter,
            SortOrder = sortOrder,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow,
        };
    }

    [Fact]
    public async Task GetFiles_ReturnsStoredMarkersAndSelection()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());
        var externalDownload = CreateExternalDownload(ContentState.Waiting);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        var result = await services.QBitContentManager.GetFiles(externalDownload.Id.ToString());

        Assert.Null(result.MonitoredChapters);
        Assert.Equal(2, result.Files.Count);
        Assert.Equal("Spice and Wolf v02.cbz", result.Files[1].Path);
        Assert.Equal("2", result.Files[1].VolumeMarker);
        Assert.False(result.Files[1].Selected);
    }

    [Fact]
    public async Task GetFiles_NormalizesDefaultMarkers()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());
        var externalDownload = CreateExternalDownload(ContentState.Waiting);
        externalDownload.Files =
        [
            new DownloadFile
            {
                FileName = "Spice and Wolf v01.cbz",
                FullPath = "Spice and Wolf v01.cbz",
                FileSize = 1024,
                VolumeMarker = "-100000",
                ChapterMarker = "-100000",
                Selected = true,
            },
        ];

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        var result = await services.QBitContentManager.GetFiles(externalDownload.Id.ToString());

        Assert.Equal(string.Empty, result.Files[0].VolumeMarker);
        Assert.Equal(string.Empty, result.Files[0].ChapterMarker);
    }

    [Fact]
    public async Task GetFiles_MonitoredSeries_ReturnsMissingAndUpcomingChapters()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        var series = CreateMonitoredSeries();
        var missing = CreateMonitoredChapter(series, "7", string.Empty, MonitoredChapterStatus.Missing, 7, "Vol 7");
        var upcoming = CreateMonitoredChapter(series, "8,1", string.Empty, MonitoredChapterStatus.Upcoming, 8.1f, "More Stories");
        var available = CreateMonitoredChapter(series, "9", string.Empty, MonitoredChapterStatus.Available, 9, "Vol 9");
        var notMonitored = CreateMonitoredChapter(series, "10", string.Empty, MonitoredChapterStatus.NotMonitored, 10, "Vol 10");
        series.Chapters = [missing, upcoming, available, notMonitored];

        unitOfWork.MonitoredSeriesRepository.Add(series);
        await unitOfWork.CommitAsync();

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.MonitoredSeriesId, series.Id);
        var externalDownload = CreateExternalDownload(ContentState.Waiting, metadata);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        var result = await services.QBitContentManager.GetFiles(externalDownload.Id.ToString());

        Assert.NotNull(result.MonitoredChapters);
        Assert.Equal(2, result.MonitoredChapters.Count);
        Assert.Contains(result.MonitoredChapters, c => c.Id == missing.Id);
        Assert.Contains(result.MonitoredChapters, c => c.Id == upcoming.Id);
        Assert.DoesNotContain(result.MonitoredChapters, c => c.Id == available.Id);
        Assert.DoesNotContain(result.MonitoredChapters, c => c.Id == notMonitored.Id);
    }

    [Fact]
    public async Task UpdateFileMarkers_WaitingDownload_UpdatesAndNormalizes()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);
        var externalDownload = CreateExternalDownload(ContentState.Waiting);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
        [
            new UpdateDownloadFileMarkersDto
            {
                Path = "Spice and Wolf v01.cbz",
                VolumeMarker = " 1 ",
                ChapterMarker = " 1-6 ",
            },
            new UpdateDownloadFileMarkersDto
            {
                Path = "Spice and Wolf v02.cbz",
                VolumeMarker = null,
                ChapterMarker = " ",
            },
        ]);

        var updated = await unitOfWork.ExternalDownloadRepository.GetById(externalDownload.Id);
        var first = updated!.Files.First(f => f.FullPath == "Spice and Wolf v01.cbz");
        var second = updated.Files.First(f => f.FullPath == "Spice and Wolf v02.cbz");

        Assert.Equal("1", first.VolumeMarker);
        Assert.Equal("1-6", first.ChapterMarker);
        Assert.Equal(string.Empty, second.VolumeMarker);
        Assert.Equal(string.Empty, second.ChapterMarker);
    }

    [Fact]
    public async Task UpdateFileMarkers_MonitoredSeries_UsesChapterMarkers()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);

        var series = CreateMonitoredSeries();
        var chapter = CreateMonitoredChapter(series, "8,1", string.Empty, MonitoredChapterStatus.Upcoming, 8.1f, "More Stories");
        series.Chapters = [chapter];
        unitOfWork.MonitoredSeriesRepository.Add(series);
        await unitOfWork.CommitAsync();

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.MonitoredSeriesId, series.Id);
        var externalDownload = CreateExternalDownload(ContentState.Waiting, metadata);
        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
        [
            new UpdateDownloadFileMarkersDto
            {
                Path = "Spice and Wolf v01.cbz",
                MonitoredChapterId = chapter.Id,
            },
        ]);

        var updated = await unitOfWork.ExternalDownloadRepository.GetById(externalDownload.Id);
        var file = updated!.Files.First(f => f.FullPath == "Spice and Wolf v01.cbz");

        Assert.Equal("8,1", file.VolumeMarker);
        Assert.Equal(string.Empty, file.ChapterMarker);
        Assert.Equal(chapter.Id, file.MonitoredChapterId);
    }

    [Fact]
    public async Task UpdateFileMarkers_MonitoredSeries_NullChapterLeavesFileUnchanged()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);

        var series = CreateMonitoredSeries();
        series.Chapters = [CreateMonitoredChapter(series, "8", string.Empty, MonitoredChapterStatus.Missing, 8, "Vol 8")];
        unitOfWork.MonitoredSeriesRepository.Add(series);
        await unitOfWork.CommitAsync();

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.MonitoredSeriesId, series.Id);
        var externalDownload = CreateExternalDownload(ContentState.Waiting, metadata);
        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
        [
            new UpdateDownloadFileMarkersDto
            {
                Path = "Spice and Wolf v01.cbz",
                MonitoredChapterId = null,
                VolumeMarker = "99",
                ChapterMarker = "99",
            },
        ]);

        var updated = await unitOfWork.ExternalDownloadRepository.GetById(externalDownload.Id);
        var file = updated!.Files.First(f => f.FullPath == "Spice and Wolf v01.cbz");

        Assert.Equal("1", file.VolumeMarker);
        Assert.Equal("1", file.ChapterMarker);
        Assert.Null(file.MonitoredChapterId);
    }

    [Fact]
    public async Task UpdateFileMarkers_MonitoredSeries_RejectsUnknownChapter()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);

        var series = CreateMonitoredSeries();
        series.Chapters = [CreateMonitoredChapter(series, "8", string.Empty, MonitoredChapterStatus.Missing, 8, "Vol 8")];
        unitOfWork.MonitoredSeriesRepository.Add(series);
        await unitOfWork.CommitAsync();

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.MonitoredSeriesId, series.Id);
        var externalDownload = CreateExternalDownload(ContentState.Waiting, metadata);
        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
            [
                new UpdateDownloadFileMarkersDto
                {
                    Path = "Spice and Wolf v01.cbz",
                    MonitoredChapterId = Guid.NewGuid(),
                },
            ]));
    }

    [Fact]
    public async Task UpdateFileMarkers_MonitoredSeries_RejectsUnavailableChapter()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);

        var series = CreateMonitoredSeries();
        var chapter = CreateMonitoredChapter(series, "9", string.Empty, MonitoredChapterStatus.Available, 9, "Vol 9");
        series.Chapters = [chapter];
        unitOfWork.MonitoredSeriesRepository.Add(series);
        await unitOfWork.CommitAsync();

        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.MonitoredSeriesId, series.Id);
        var externalDownload = CreateExternalDownload(ContentState.Waiting, metadata);
        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
            [
                new UpdateDownloadFileMarkersDto
                {
                    Path = "Spice and Wolf v01.cbz",
                    MonitoredChapterId = chapter.Id,
                },
            ]));
    }

    [Fact]
    public async Task UpdateFileMarkers_UnknownPath_ThrowsAndLeavesMarkersUnchanged()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);
        var externalDownload = CreateExternalDownload(ContentState.Waiting);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
            [
                new UpdateDownloadFileMarkersDto
                {
                    Path = "missing.cbz",
                    VolumeMarker = "9",
                },
            ]));

        var unchanged = await unitOfWork.ExternalDownloadRepository.GetById(externalDownload.Id);
        Assert.Equal("1", unchanged!.Files[0].VolumeMarker);
    }

    [Fact]
    public async Task UpdateFileMarkers_DuplicatePath_Throws()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);
        var externalDownload = CreateExternalDownload(ContentState.Waiting);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
            [
                new UpdateDownloadFileMarkersDto { Path = "Spice and Wolf v01.cbz", VolumeMarker = "1" },
                new UpdateDownloadFileMarkersDto { Path = "Spice and Wolf v01.cbz", VolumeMarker = "2" },
            ]));
    }

    [Fact]
    public async Task UpdateFileMarkers_NotWaiting_Throws()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);
        var externalDownload = CreateExternalDownload(ContentState.Downloading);

        unitOfWork.ExternalDownloadRepository.Add(externalDownload);
        await unitOfWork.CommitAsync();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            services.QBitContentManager.UpdateFileMarkers(externalDownload.Id.ToString(),
            [
                new UpdateDownloadFileMarkersDto
                {
                    Path = "Spice and Wolf v01.cbz",
                    VolumeMarker = "9",
                },
            ]));
    }
}
