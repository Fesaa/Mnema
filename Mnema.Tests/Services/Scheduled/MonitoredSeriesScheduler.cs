using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Services.Scheduled;
using NSubstitute;
using Xunit.Abstractions;

namespace Mnema.Tests.Services.Scheduled;

public class MonitoredSeriesSchedulerTest(ITestOutputHelper testOutputHelper) : DatabaseTests(testOutputHelper)
{
    #region FindMatch

    [Fact]
    public async Task FindMatch_ExternalIdMatch_ReturnsMonitoredSeries()
    {
        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "123");

        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>());

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Same(series, match);
    }

    [Fact]
    public async Task FindMatch_ExternalIdMismatch_ContinuesAndReturnsNull()
    {
        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "456");

        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>());

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatch_ExternalIdMatch_ChapterNotMonitored_ReturnsNull()
    {
        var series = CreateMonitoredSeries(externalId: "123");
        series.Chapters = [new MonitoredChapter { ExternalId = "release-1", Status = MonitoredChapterStatus.NotMonitored }];

        var release = CreateRelease(releaseId: "release-1", contentId: "123");

        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>());

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatch_ProviderMismatch_IsIgnored()
    {
        var series = CreateMonitoredSeries(externalId: "123", provider: Provider.Nyaa);
        var release = CreateRelease(releaseId: "release-1", contentId: "123", provider: Provider.Mangadex);

        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>());

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatch_TitleAndFormatMatch_HasMissingChapter_ReturnsMatch()
    {
        const Format format = Format.Archive;

        var series = CreateMonitoredSeries();
        series.ValidTitles = ["Spice and Wolf"];
        series.Format = format;
        series.Chapters = [new MonitoredChapter { Volume = "1", Status = MonitoredChapterStatus.Missing }];

        var release = CreateRelease(releaseId: "release-1", contentId: null);
        release.ContentName = "Spice and Wolf";
        release.DownloadUrl = "https://example.com/1.torrent";

        var file = new TorrentFile("Spice And Wolf Vol 1.cbz", "Spice And Wolf Vol 1.cbz", 0);

        var parser = Substitute.For<IParserService>();
        parser.FullParse(release.ContentName, series.ContentFormat)
            .Returns(new ParseResult(release.ContentName, ["Spice and Wolf"], new NumberRange("", 0, 0), new NumberRange("", 0, 0)));
        parser.FullParse(file.FileName, series.ContentFormat)
            .Returns(new ParseResult(file.FileName, ["Spice and Wolf"], new NumberRange("1", 1, 1), new NumberRange("", 0, 0)));
        parser.ParseFormat(file.FileName).Returns(format);
        parser.FindMatch(series.Chapters, Arg.Any<IHasPositionMarkers>()).Returns(series.Chapters[0]);

        var scanner = Substitute.For<IScannerService>();
        scanner.ParseTorrentFile(release.DownloadUrl, Arg.Any<CancellationToken>())
            .Returns(new ParsedTorrentInfo("1MB", [file]));

        var scope = CreateScope(parser, scanner);

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Same(series, match);
    }

    [Fact]
    public async Task FindMatch_NoTitleMatch_ContinuesAndReturnsNull()
    {
        var series = CreateMonitoredSeries();
        series.ValidTitles = ["Spice and Wolf"];

        var release = CreateRelease(releaseId: "release-1", contentId: null);
        release.ContentName = "A Completely Different Series";

        var parser = Substitute.For<IParserService>();
        parser.FullParse(release.ContentName, series.ContentFormat)
            .Returns(new ParseResult(release.ContentName, ["A Completely Different Series"], new NumberRange("", 0, 0), new NumberRange("", 0, 0)));

        var scope = CreateScope(parser, Substitute.For<IScannerService>());

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
        // Should never get as far as parsing the torrent file since the title didn't match
        await scope.ServiceProvider.GetRequiredService<IScannerService>()
            .DidNotReceiveWithAnyArgs().ParseTorrentFile(default!, default);
    }

    [Fact]
    public async Task FindMatch_FormatMismatch_ContinuesAndReturnsNull()
    {
        var series = CreateMonitoredSeries();
        series.ValidTitles = ["Spice and Wolf"];
        series.Format = (Format)1;
        series.Chapters = [new MonitoredChapter { Volume = "1", Status = MonitoredChapterStatus.Missing }];

        var release = CreateRelease(releaseId: "release-1", contentId: null);
        release.ContentName = "Spice and Wolf";
        release.DownloadUrl = "https://example.com/1.torrent";

        var file = new TorrentFile("Spice And Wolf Vol 1.cbz", "Spice And Wolf Vol 1.cbz", 0);

        var parser = Substitute.For<IParserService>();
        parser.FullParse(release.ContentName, series.ContentFormat)
            .Returns(new ParseResult(release.ContentName, ["Spice and Wolf"], new NumberRange("", 0, 0), new NumberRange("", 0, 0)));
        parser.FullParse(file.FileName, series.ContentFormat)
            .Returns(new ParseResult(file.FileName, ["Spice and Wolf"], new NumberRange("1", 1, 1), new NumberRange("", 0, 0)));
        // Parsed format does not match the monitored series' expected format
        parser.ParseFormat(file.FileName).Returns((Format)2);

        var scanner = Substitute.For<IScannerService>();
        scanner.ParseTorrentFile(release.DownloadUrl, Arg.Any<CancellationToken>())
            .Returns(new ParsedTorrentInfo("1MB", [file]));

        var scope = CreateScope(parser, scanner);

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
    }

    [Fact]
    public async Task FindMatch_AllChaptersAlreadyAvailableOrNotMonitored_SkipsAndReturnsNull()
    {
        const Format format = Format.Archive;

        var series = CreateMonitoredSeries();
        series.ValidTitles = ["Spice and Wolf"];
        series.Format = format;
        series.Chapters =
        [
            new MonitoredChapter { Volume = "1", Status = MonitoredChapterStatus.Available },
            new MonitoredChapter { Volume = "2", Status = MonitoredChapterStatus.NotMonitored }
        ];

        var release = CreateRelease(releaseId: "release-1", contentId: null);
        release.ContentName = "Spice and Wolf";
        release.DownloadUrl = "https://example.com/1.torrent";

        var file = new TorrentFile("Spice And Wolf Vol 1.cbz", "Spice And Wolf Vol 1.cbz", 0);

        var parser = Substitute.For<IParserService>();
        parser.FullParse(release.ContentName, series.ContentFormat)
            .Returns(new ParseResult(release.ContentName, ["Spice and Wolf"], new NumberRange("", 0, 0), new NumberRange("", 0, 0)));
        parser.FullParse(file.FileName, series.ContentFormat)
            .Returns(new ParseResult(file.FileName, ["Spice and Wolf"], new NumberRange("1", 1, 1), new NumberRange("", 0, 0)));
        parser.ParseFormat(file.FileName).Returns(format);
        // Every chapter found on the torrent already matches an Available/NotMonitored chapter
        parser.FindMatch(series.Chapters, Arg.Any<IHasPositionMarkers>()).Returns(series.Chapters[0]);

        var scanner = Substitute.For<IScannerService>();
        scanner.ParseTorrentFile(release.DownloadUrl, Arg.Any<CancellationToken>())
            .Returns(new ParsedTorrentInfo("1MB", [file]));

        var scope = CreateScope(parser, scanner);

        var match = await MonitoredSeriesScheduler.FindMatch(scope, [series], release, CancellationToken.None);

        Assert.Null(match);
    }

    #endregion

    #region ProcessMonitoredReleases

    [Fact]
    public async Task ProcessMonitoredReleases_NewMatch_StartsDownload()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "123");

        var downloadService = Substitute.For<IDownloadService>();
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release], [series], CancellationToken.None);

        await downloadService.Received(1).StartDownload(Arg.Is<DownloadRequestDto>(d =>
            d.Provider == release.Provider && d.Id == release.ContentId));
        Assert.Equal(1, result.StartedDownloads);
        Assert.Equal(0, result.FailedDownloads);
        Assert.Single(result.Releases);
    }

    [Fact]
    public async Task ProcessMonitoredReleases_NoMatch_IsSkipped()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "does-not-match");

        var downloadService = Substitute.For<IDownloadService>();
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release], [series], CancellationToken.None);

        await downloadService.DidNotReceiveWithAnyArgs().StartDownload(default!);
        Assert.Equal(0, result.StartedDownloads);
        Assert.Empty(result.Releases);
    }

    [Fact]
    public async Task ProcessMonitoredReleases_SameContentIdTwice_OnlyDownloadsOnce_ButCountsBothProcessed()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        // Two distinct monitored series that both resolve to the same underlying content id.
        var seriesA = CreateMonitoredSeries(externalId: "abc");
        var seriesB = CreateMonitoredSeries(externalId: "abc");

        var release1 = CreateRelease(releaseId: "r1", contentId: "abc");
        var release2 = CreateRelease(releaseId: "r2", contentId: "abc");

        var downloadService = Substitute.For<IDownloadService>();
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release1, release2], [seriesA, seriesB], CancellationToken.None);

        // Only the first release actually triggers a download; the second is recognized
        // as already-in-progress content and skipped, but is still counted as "processed".
        await downloadService.Received(1).StartDownload(Arg.Any<DownloadRequestDto>());
        Assert.Equal(2, result.StartedDownloads);
        Assert.Equal(2, result.Releases.Count);
    }

    [Fact]
    public async Task ProcessMonitoredReleases_SameContentIdTwice_GroupedRelease_DownloadsBoth()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        var seriesA = CreateMonitoredSeries(externalId: "abc");
        var seriesB = CreateMonitoredSeries(externalId: "abc");

        var release1 = CreateRelease(releaseId: "r1", contentId: "abc");
        var release2 = CreateRelease(releaseId: "r2", contentId: "abc");
        release2.IsGroupedRelease = true;

        var downloadService = Substitute.For<IDownloadService>();
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release1, release2], [seriesA, seriesB], CancellationToken.None);

        await downloadService.Received(2).StartDownload(Arg.Any<DownloadRequestDto>());
        Assert.Equal(2, result.StartedDownloads);
    }

    [Fact]
    public async Task ProcessMonitoredReleases_ContentAlreadyDownloading_SkipsButCountsAsProcessed()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "123");

        var downloadService = Substitute.For<IDownloadService>();
        downloadService.HasContent(release.Provider, release.ContentId!).Returns(true);
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release], [series], CancellationToken.None);

        await downloadService.DidNotReceive().StartDownload(Arg.Any<DownloadRequestDto>());
        Assert.Equal(1, result.StartedDownloads);
        Assert.Single(result.Releases);
    }

    [Fact]
    public async Task ProcessMonitoredReleases_DownloadThrows_CountsFailedAndCommunicatesException()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out _);

        var series = CreateMonitoredSeries(externalId: "123");
        var release = CreateRelease(releaseId: "release-1", contentId: "123");

        var downloadService = Substitute.For<IDownloadService>();
        downloadService.StartDownload(Arg.Any<DownloadRequestDto>()).Returns(Task.FromException(new Exception("boom")));
        var connectionService = Substitute.For<IConnectionService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(), downloadService, connectionService);

        var result = await scheduler.ProcessMonitoredReleases(scope, [release], [series], CancellationToken.None);

        Assert.Equal(0, result.StartedDownloads);
        Assert.Equal(1, result.FailedDownloads);
        Assert.Empty(result.Releases);
        connectionService.Received(1).CommunicateException(Arg.Any<string>(), Arg.Any<Exception>());
    }

    #endregion

    #region RunWatcher

    [Fact]
    public async Task RunWatcher_NoMonitoredSeries_DoesNotSearch()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out var scopeFactory);

        var searchService = Substitute.For<ISearchService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(),
            Substitute.For<IDownloadService>(), Substitute.For<IConnectionService>(), unitOfWork, searchService);
        scopeFactory.CreateScope().Returns(scope);

        await scheduler.RunWatcher(CancellationToken.None);

        await searchService.DidNotReceiveWithAnyArgs().SearchReleases(default!, default);
    }

    [Fact]
    public async Task RunWatcher_NoReleasesFound_DoesNotPersistAnything()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out var scopeFactory);

        ctx.MonitoredSeries.Add(CreateMonitoredSeries(externalId: "123"));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var searchService = Substitute.For<ISearchService>();
        searchService.SearchReleases(Arg.Any<List<Provider>>(), Arg.Any<CancellationToken>()).Returns([]);

        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(),
            Substitute.For<IDownloadService>(), Substitute.For<IConnectionService>(), unitOfWork, searchService);
        scopeFactory.CreateScope().Returns(scope);

        await scheduler.RunWatcher(CancellationToken.None);

        Assert.Empty(await ctx.ContentReleases.ToListAsync());
    }

    [Fact]
    public async Task RunWatcher_ReleaseAlreadyProcessed_IsFilteredOut()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out var scopeFactory);

        ctx.MonitoredSeries.Add(CreateMonitoredSeries(externalId: "123"));
        ctx.ContentReleases.Add(new ContentRelease { Provider = Provider.Nyaa, ReleaseId = "r1" });
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var searchService = Substitute.For<ISearchService>();
        searchService.SearchReleases(Arg.Any<List<Provider>>(), Arg.Any<CancellationToken>())
            .Returns([CreateRelease(releaseId: "r1", contentId: "123")]);

        var downloadService = Substitute.For<IDownloadService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(),
            downloadService, Substitute.For<IConnectionService>(), unitOfWork, searchService);
        scopeFactory.CreateScope().Returns(scope);

        await scheduler.RunWatcher(CancellationToken.None);

        await downloadService.DidNotReceiveWithAnyArgs().StartDownload(default!);
        Assert.Single(await ctx.ContentReleases.ToListAsync());
    }

    [Fact]
    public async Task RunWatcher_NewRelease_StartsDownloadAndPersistsRelease()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();
        var scheduler = CreateScheduler(unitOfWork, out var scopeFactory);

        ctx.MonitoredSeries.Add(CreateMonitoredSeries(externalId: "123"));
        await ctx.SaveChangesAsync();
        ctx.ChangeTracker.Clear();

        var release = CreateRelease(releaseId: "r-new", contentId: "123");

        var searchService = Substitute.For<ISearchService>();
        searchService.SearchReleases(Arg.Any<List<Provider>>(), Arg.Any<CancellationToken>()).Returns([release]);

        var downloadService = Substitute.For<IDownloadService>();
        var scope = CreateScope(Substitute.For<IParserService>(), Substitute.For<IScannerService>(),
            downloadService, Substitute.For<IConnectionService>(), unitOfWork, searchService);
        scopeFactory.CreateScope().Returns(scope);

        await scheduler.RunWatcher(CancellationToken.None);

        await downloadService.Received(1).StartDownload(Arg.Any<DownloadRequestDto>());

        var saved = await ctx.ContentReleases.ToListAsync();
        Assert.Single(saved);
        Assert.Equal("r-new", saved[0].ReleaseId);
    }

    #endregion

    #region Helpers

    private static MonitoredSeries CreateMonitoredSeries(string externalId = "", Provider provider = Provider.Nyaa) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Spice and Wolf",
        NormalizedTitle = "Spice and Wolf".ToNormalized(),
        Summary = string.Empty,
        BaseDir = string.Empty,
        Provider = provider,
        HardcoverId = string.Empty,
        MangaBakaId = string.Empty,
        TitleOverride = string.Empty,
        ExternalId = externalId,
        ValidTitles = [],
        Chapters = [],
        Metadata = new MetadataBag()
    };

    private static ContentRelease CreateRelease(string releaseId, string? contentId, Provider provider = Provider.Nyaa) => new()
    {
        Provider = provider,
        ReleaseId = releaseId,
        ContentId = contentId,
    };

    private MonitoredSeriesScheduler CreateScheduler(IUnitOfWork unitOfWork, out IServiceScopeFactory scopeFactory)
    {
        scopeFactory = Substitute.For<IServiceScopeFactory>();

        return new MonitoredSeriesScheduler(
            Substitute.For<ILogger<MonitoredSeriesScheduler>>(),
            scopeFactory,
            Substitute.For<IRecurringJobManagerV2>(),
            Substitute.For<IWebHostEnvironment>(),
            unitOfWork);
    }

    private static IServiceScope CreateScope(
        IParserService parser,
        IScannerService scanner,
        IDownloadService? downloadService = null,
        IConnectionService? connectionService = null,
        IUnitOfWork? unitOfWork = null,
        ISearchService? searchService = null)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IParserService)).Returns(parser);
        provider.GetService(typeof(IScannerService)).Returns(scanner);

        if (downloadService != null)
            provider.GetService(typeof(IDownloadService)).Returns(downloadService);

        if (connectionService != null)
            provider.GetService(typeof(IConnectionService)).Returns(connectionService);

        if (unitOfWork != null)
            provider.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);

        if (searchService != null)
            provider.GetService(typeof(ISearchService)).Returns(searchService);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        return scope;
    }

    #endregion
}
