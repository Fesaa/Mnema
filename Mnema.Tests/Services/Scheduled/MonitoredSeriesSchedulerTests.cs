using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;
using Mnema.Services.Scheduled;
using NSubstitute;

namespace Mnema.Tests.Services.Scheduled;

public class MonitoredSeriesSchedulerTests
{

    private static IServiceScope BuildScope(IParserService parserService, IScannerService scannerService)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IParserService)).Returns(parserService);
        provider.GetService(typeof(IScannerService)).Returns(scannerService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        return scope;
    }

    private static IParserService MakeParserService(
        ParseResult? parseResult = null,
        Format parsedFormat = Format.Archive)
    {
        var svc = Substitute.For<IParserService>();
        svc.FullParse(Arg.Any<string>(), Arg.Any<ContentFormat>())
           .Returns(parseResult ?? new ParseResult("", [], default, default));
        svc.ParseFormat(Arg.Any<string>()).Returns(parsedFormat);
        return svc;
    }

    private static IScannerService MakeScannerService(
        List<Chapter>? chapters = null,
        ContentFormat format = ContentFormat.Manga)
    {
        var svc = Substitute.For<IScannerService>();
        svc.ParseTorrentFile(Arg.Any<string>(), Arg.Any<ContentFormat>(), Arg.Any<CancellationToken>())
           .Returns(new TorrentScanResult("0 MB", chapters ?? []));
        svc.FindMatch(Arg.Any<List<MonitoredChapter>>(), Arg.Any<Chapter>())
           .Returns((MonitoredChapter?)null);
        return svc;
    }

    private static Chapter MakeChapter(string title = "Series Vol.1") => new()
    {
        Id = Guid.NewGuid().ToString(),
        Title = title,
        VolumeMarker = "1",
        ChapterMarker = "",
        Tags = [],
        People = [],
        TranslationGroups = [],
    };

    private static ContentRelease MakeRelease(
        string releaseName = "Series Vol.1",
        string releaseId = "rel-1",
        string? contentId = null,
        Provider provider = Provider.Nyaa,
        string downloadUrl = "http://example.com/file.torrent")
        => new()
        {
            ReleaseName = releaseName,
            ReleaseId = releaseId,
            ContentId = contentId,
            ContentName = releaseName,
            Provider = provider,
            DownloadUrl = downloadUrl,
        };

    private static MonitoredSeries MakeSeries(
        string title = "Series",
        Provider provider = Provider.Nyaa,
        string? externalId = null,
        Format format = Format.Archive,
        List<MonitoredChapter>? chapters = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            ValidTitles =
            [
                title
            ],
            Provider = provider,
            ExternalId = externalId,
            Format = format,
            ContentFormat = ContentFormat.Manga,
            Chapters = chapters ??
            [
            ],
            BaseDir = string.Empty,
        };

    private static MonitoredChapter MakeMonitoredChapter(
        MonitoredChapterStatus status,
        string externalId = "rel-1")
        => new()
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Status = status,
            Title = "Chapter 1",
            Summary = "",
            Volume = "1",
            Chapter = "1",
        };

    [Fact]
    public async Task FindMatch_ExternalId_ReturnsMatch_WhenIdsAlign()
    {
        var series = MakeSeries(externalId: "ext-42");
        var release = MakeRelease(contentId: "ext-42");

        var scope = BuildScope(MakeParserService(), MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
    }

    [Fact]
    public async Task FindMatch_ExternalId_ReturnsNull_WhenContentIdDoesNotMatch()
    {
        var series = MakeSeries(externalId: "ext-42");
        var release = MakeRelease(contentId: "ext-99");

        var scope = BuildScope(MakeParserService(), MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_ExternalId_ReturnsNull_WhenMatchingChapterIsNotMonitored()
    {
        // release.ReleaseId == chapter.ExternalId → chapter status governs the outcome
        var chapter = MakeMonitoredChapter(MonitoredChapterStatus.NotMonitored, externalId: "rel-1");
        var series = MakeSeries(externalId: "ext-42", chapters: [chapter]);
        var release = MakeRelease(contentId: "ext-42", releaseId: "rel-1");

        var scope = BuildScope(MakeParserService(), MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_ExternalId_ReturnsMatch_WhenMatchingChapterIsMissing()
    {
        var chapter = MakeMonitoredChapter(MonitoredChapterStatus.Missing, externalId: "rel-1");
        var series = MakeSeries(externalId: "ext-42", chapters: [chapter]);
        var release = MakeRelease(contentId: "ext-42", releaseId: "rel-1");

        var scope = BuildScope(MakeParserService(), MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
    }

    [Fact]
    public async Task FindMatch_Title_ReturnsMatch_WhenNormalizedTitleMatches()
    {
        var series = MakeSeries(title: "My Series");
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var chapter = MakeChapter("My Series Vol.1");
        var scannerSvc = MakeScannerService([chapter]);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
    }

    [Fact]
    public async Task FindMatch_Title_ReturnsNull_WhenTitleDoesNotMatch()
    {
        var series = MakeSeries(title: "My Series");
        var release = MakeRelease(releaseName: "Completely Different Title Vol.1");

        var parseResult = new ParseResult("Completely Different Title Vol.1", ["Completely Different Title"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var scope = BuildScope(parserSvc, MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_Title_ReturnsNull_WhenProviderDoesNotMatch()
    {
        var series = MakeSeries(title: "My Series", provider: Provider.Nyaa);
        var release = MakeRelease(releaseName: "My Series Vol.1", provider: Provider.Mangadex);

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var scope = BuildScope(parserSvc, MakeScannerService());

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_Format_ReturnsNull_WhenTorrentFormatDoesNotMatchSeries()
    {
        var series = MakeSeries(title: "My Series", format: Format.Archive);
        var release = MakeRelease(releaseName: "My Series Vol.1 LN");

        var parseResult = new ParseResult("My Series Vol.1 LN", ["My Series"], default, default);
        // Parser identifies each chapter as Epub, but the series wants Archive
        var parserSvc = MakeParserService(parseResult, parsedFormat: Format.Epub);

        var chapter = MakeChapter("My Series Vol.1 LN");
        var scannerSvc = MakeScannerService([chapter]);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_Format_ReturnsMatch_WhenTorrentFormatMatchesSeries()
    {
        var series = MakeSeries(title: "My Series", format: Format.Archive);
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult, parsedFormat: Format.Archive);

        var chapter = MakeChapter("My Series Vol.1");
        var scannerSvc = MakeScannerService([chapter]);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.NotNull(result);
    }

    // ---------------------------------------------------------------------------
    // Skip when all chapters are Available or NotMonitored
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task FindMatch_SkipsDownload_WhenAllTorrentChaptersAreAlreadyAvailable()
    {
        var monitoredChapter = MakeMonitoredChapter(MonitoredChapterStatus.Available);
        var series = MakeSeries(title: "My Series", chapters: [monitoredChapter]);
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var chapter = MakeChapter("My Series Vol.1");
        var scannerSvc = MakeScannerService([chapter]);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), chapter)
                  .Returns(monitoredChapter);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_SkipsDownload_WhenAllTorrentChaptersAreNotMonitored()
    {
        var monitoredChapter = MakeMonitoredChapter(MonitoredChapterStatus.NotMonitored);
        var series = MakeSeries(title: "My Series", chapters: [monitoredChapter]);
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var chapter = MakeChapter("My Series Vol.1");
        var scannerSvc = MakeScannerService([chapter]);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), chapter)
                  .Returns(monitoredChapter);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_SkipsDownload_WhenAllTorrentChaptersAreMixOfAvailableAndNotMonitored()
    {
        var availableChapter = MakeMonitoredChapter(MonitoredChapterStatus.Available, "rel-1");
        var notMonitoredChapter = MakeMonitoredChapter(MonitoredChapterStatus.NotMonitored, "rel-2");
        var series = MakeSeries(title: "My Series", chapters: [availableChapter, notMonitoredChapter]);
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var chapter1 = MakeChapter("My Series Vol.1");
        var chapter2 = MakeChapter("My Series Vol.2");
        var scannerSvc = MakeScannerService([chapter1, chapter2]);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), chapter1).Returns(availableChapter);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), chapter2).Returns(notMonitoredChapter);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindMatch_DoesNotSkip_WhenAtLeastOneTorrentChapterIsMissing()
    {
        var availableChapter = MakeMonitoredChapter(MonitoredChapterStatus.Available, "rel-1");
        var series = MakeSeries(title: "My Series", chapters: [availableChapter]);
        var release = MakeRelease(releaseName: "My Series Vol.1");

        var parseResult = new ParseResult("My Series Vol.1", ["My Series"], default, default);
        var parserSvc = MakeParserService(parseResult);

        var knownChapter = MakeChapter("My Series Vol.1");
        var newChapter = MakeChapter("My Series Vol.2");  // not in monitoredChapters → null from FindMatch
        var scannerSvc = MakeScannerService([knownChapter, newChapter]);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), knownChapter).Returns(availableChapter);
        scannerSvc.FindMatch(Arg.Any<List<MonitoredChapter>>(), newChapter).Returns((MonitoredChapter?)null);

        var scope = BuildScope(parserSvc, scannerSvc);

        var result = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetProviders_ExcludesDisabledProviders()
    {
        var entities = new List<MonitoredSeries>
        {
            new()
            {
                Provider = Provider.Mangadex,
                Title = string.Empty,
                BaseDir = string.Empty
            },
            new()
            {
                Provider = Provider.Nyaa,
                Title = string.Empty,
                BaseDir = string.Empty
            }
        };

        var settings = new List<ProviderSettings>
        {
            new() { Provider = Provider.Mangadex, Settings = new MetadataBag()},
            new() { Provider = Provider.Nyaa, Settings = new MetadataBag() },
        };

        settings.Last().Settings.SetKey(ProviderSettings.Disable, true);

        var unitOfWorkMock = Substitute.For<IUnitOfWork>();
        var providerSettingsRepositoryMock = Substitute.For<IProviderSettingsRepository>();
        unitOfWorkMock.ProviderSettingsRepository.Returns(providerSettingsRepositoryMock);
        providerSettingsRepositoryMock.GetAllSettings(Arg.Any<CancellationToken>()).Returns(settings);

        var sut = new TestableMonitoredSeriesScheduler(
            Substitute.For<ILogger<MonitoredSeriesScheduler>>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IRecurringJobManagerV2>(),
            Substitute.For<IWebHostEnvironment>(),
            unitOfWorkMock);

        var result = await sut.InvokeGetProviders(entities);

        Assert.Single(result);
        Assert.Equal(Provider.Mangadex, result[0]);
    }

    private class TestableMonitoredSeriesScheduler(
        ILogger<MonitoredSeriesScheduler> logger,
        IServiceScopeFactory scopeFactory,
        IRecurringJobManagerV2 recurringJobManager,
        IWebHostEnvironment environment,
        IUnitOfWork unitOfWork
    ) : MonitoredSeriesScheduler(logger, scopeFactory, recurringJobManager, environment, unitOfWork)
    {
        public Task<List<Provider>> InvokeGetProviders(List<MonitoredSeries> entities) =>
            GetProviders(entities);
    }
}
