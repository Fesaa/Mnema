using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Services.Scheduled;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Mnema.Tests.Services.Scheduled;

[TestSubject(typeof(SubscriptionScheduler))]
public class SubscriptionSchedulerTests
{

    private static ContentRelease CreateContentRelease(string releaseId, string contentId = "")
    {
        return new ContentRelease
        {
            ReleaseId = releaseId,
            ContentId = contentId,
            Provider = Provider.Nyaa,
        };
    }

    private static Subscription CreateSubscription(string contentId)
    {
        return new Subscription
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            Title = string.Empty,
            BaseDir = string.Empty,
            Provider = Provider.Nyaa,
            Metadata = new MetadataBag(),
            Status = SubscriptionStatus.Enabled
        };
    }

    private static SubscriptionScheduler CreateScheduler()
    {
        return new SubscriptionScheduler(
            Substitute.For<ILogger<SubscriptionScheduler>>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IRecurringJobManagerV2>(),
            Substitute.For<IWebHostEnvironment>()
        );
    }

    [Fact]
    public async Task FilterProcessedReleasesTest()
    {
        var contentReleaseRepository = Substitute.For<IContentReleaseRepository>();
        contentReleaseRepository
            .FilterReleases(Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(["3"]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ContentReleaseRepository.Returns(contentReleaseRepository);

        List<ContentRelease> releases = [
            CreateContentRelease("1"),
            CreateContentRelease("2"),
            CreateContentRelease("3"),
        ];

        var newReleases = (await SubscriptionScheduler
            .FilterProcessedReleases(unitOfWork, releases, CancellationToken.None))
            .Select(x => x.ReleaseId)
            .ToList();

        Assert.Single(newReleases);
        Assert.Equal("3", newReleases[0]);
    }

    [Fact]
    public async Task ProcessSubscriptions_WithMatchingSubscriptions_StartsDownloads()
    {
        var downloadService = Substitute.For<IDownloadService>();

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1"),
            CreateContentRelease("release2", "content2")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content1"),
            CreateSubscription("content2")
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, CancellationToken.None);

        Assert.Equal(2, result.StartedDownloads);
        Assert.Equal(0, result.FailedDownloads);
        Assert.Equal(2, result.Releases.Count);
        await downloadService.Received(2).StartDownload(Arg.Any<DownloadRequestDto>());
    }

    [Fact]
    public async Task ProcessSubscriptions_WithNoMatchingSubscriptions_ReturnsEmptyResult()
    {
        var downloadService = Substitute.For<IDownloadService>();

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content2")
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, CancellationToken.None);

        Assert.Equal(0, result.StartedDownloads);
        Assert.Equal(0, result.FailedDownloads);
        Assert.Empty(result.Releases);
        await downloadService.DidNotReceive().StartDownload(Arg.Any<DownloadRequestDto>());
    }

    [Fact]
    public async Task ProcessSubscriptions_WithMultipleReleasesForSameContent_ReturnsAllMatchingReleases()
    {
        var downloadService = Substitute.For<IDownloadService>();

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1"),
            CreateContentRelease("release2", "content1"),
            CreateContentRelease("release3", "content1")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content1"),
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, CancellationToken.None);


        Assert.Equal(3, result.Releases.Count);
        Assert.All(result.Releases, r => Assert.Equal("content1", r.ContentId));

        // But only one download was started
        Assert.Equal(1, result.StartedDownloads);
        await downloadService.Received(1).StartDownload(Arg.Any<DownloadRequestDto>());
    }

    [Fact]
    public async Task ProcessSubscriptions_WithDownloadFailure_IncrementsFailedCount()
    {
        var downloadService = Substitute.For<IDownloadService>();
        downloadService
            .StartDownload(Arg.Any<DownloadRequestDto>())
            .ThrowsAsync(new Exception("Download failed"));

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content1"),
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, CancellationToken.None);

        Assert.Equal(0, result.StartedDownloads);
        Assert.Equal(1, result.FailedDownloads);
        Assert.Empty(result.Releases);
    }

    [Fact]
    public async Task ProcessSubscriptions_WithPartialFailures_TracksSuccessAndFailureSeparately()
    {
        var downloadService = Substitute.For<IDownloadService>();
        downloadService
            .StartDownload(Arg.Is<DownloadRequestDto>(dto => dto.Id == "content2"))
            .ThrowsAsync(new Exception("Download failed"));

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1"),
            CreateContentRelease("release2", "content2")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content1"),
            CreateSubscription("content2")
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, CancellationToken.None);

        Assert.Equal(1, result.StartedDownloads);
        Assert.Equal(1, result.FailedDownloads);
        Assert.Single(result.Releases);
        Assert.Equal("content1", result.Releases[0].ContentId);
    }

    [Fact]
    public async Task ProcessSubscriptions_WithCancellation_StopsProcessing()
    {
        var downloadService = Substitute.For<IDownloadService>();
        var cts = new CancellationTokenSource();

        downloadService
            .StartDownload(Arg.Any<DownloadRequestDto>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => cts.Cancel());

        List<ContentRelease> releases = [
            CreateContentRelease("release1", "content1"),
            CreateContentRelease("release2", "content2"),
            CreateContentRelease("release3", "content3")
        ];

        List<Subscription> subscriptions = [
            CreateSubscription("content1"),
            CreateSubscription("content2"),
            CreateSubscription("content3")
        ];

        var scheduler = CreateScheduler();

        var result = await scheduler.ProcessSubscriptions(
            downloadService, releases, subscriptions, cts.Token);

        Assert.Equal(1, result.StartedDownloads);
        Assert.Equal(0, result.FailedDownloads); // canceled breaks
        await downloadService.Received(1).StartDownload(Arg.Any<DownloadRequestDto>());
    }

}
