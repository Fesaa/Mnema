using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Providers.Managers.QBit;
using Mnema.Services;
using NSubstitute;
using QBittorrent.Client;

namespace Mnema.Tests.Providers.Managers;

public partial class QBitContentManagerTest
{

    private const string SpiceAndWolfHash = "ac7a2015420a6a2b22677e101adf3be7741d7c44";

    #region Duplicate requests

    [Fact]
    public async Task DownloadTorrent_DuplicatedRequests()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateServices(unitOfWork);

        service.QBitClient.GetTorrentsAsync(Arg.Any<TorrentListQuery>(), Arg.Any<CancellationToken>())
            .Returns([new TorrentInfo() { Hash = SpiceAndWolfHash }]);

        await Assert.ThrowsAsync<MnemaException>(async () => await service.QBitContentManager.Download(CreateDownloadRequestDto()));
    }

    [Fact]
    public async Task DownloadTorrent_DuplicateRequests_AllowGrouped()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var service = CreateServices(unitOfWork);

        service.QBitClient.GetTorrentsAsync(Arg.Any<TorrentListQuery>(), Arg.Any<CancellationToken>())
            .Returns([new TorrentInfo() { Hash = SpiceAndWolfHash }]);

        await service.QBitContentManager.Download(CreateDownloadRequestDto(new MetadataBag()
        {
            [RequestConstants.IsGroupedDownload.Key] = ["true"],
            [RequestConstants.HardcoverSeriesIdKey.Key] = ["53521"]
        }));
    }

    #endregion

    #region Downloading - pure methods

    [Fact]
    public void ParseSeriesFiles_GroupedSeriesFilter()
    {
        var metadata = new MetadataBag();
        metadata.SetKey(RequestConstants.IsGroupedDownload, true);

        var request = CreateDownloadRequestDto(metadata);
        HashSet<string> titles = ["Spice and Wolf".ToNormalized(), "Ookami to Koushinryou".ToNormalized()];

        var parsedFiles = QBitContentManager.ParseSeriesFiles(
            request,
            [
                new TorrentFile("Spice And Wolf Vol. 10.cbz", string.Empty, 0),
                new TorrentFile("Spice And Wolf Vol. 11.cbz", string.Empty, 0),
                new TorrentFile("Ookami to Koushinryou Vol. 12.cbz", string.Empty, 0),
                new TorrentFile("A Love Yet to Bloom Vol. 1.cbz", string.Empty, 0),
            ],
            titles,
            new ParserService()
        );

        Assert.Equal(3, parsedFiles.Count);
    }

    [Fact]
    public async Task FilterFilesToDownload_FiltersNotMonitored()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        services.ScannerService
            .ScanDirectory(Arg.Any<string>(), Arg.Any<ContentFormat>(), Arg.Any<Format>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var mSeries = new MonitoredSeries
        {
            Title = "Spice and Wolf",
            BaseDir = string.Empty,
            Chapters = [
                new MonitoredChapter
                {
                    Volume = "1",
                    Status = MonitoredChapterStatus.Missing,
                }
            ]
        };

        var file = new TorrentFile("Spice And Wolf Vol. 1.cbz", string.Empty, 0);
        List<QBitContentManager.ParsedTorrentFile> seriesFiles = [
            new(file, services.ParserService.FullParse(file.FileName, ContentFormat.Manga))
        ];

        var filteredFiles = services.QBitContentManager.FilterFilesToDownload(CreateDownloadRequestDto(), string.Empty,
            seriesFiles, mSeries, services.ToResolvedSeries(), CancellationToken.None);

        Assert.Single(filteredFiles);

        mSeries.Chapters[0].Status = MonitoredChapterStatus.NotMonitored;

        filteredFiles = services.QBitContentManager.FilterFilesToDownload(CreateDownloadRequestDto(), string.Empty,
            seriesFiles, mSeries, services.ToResolvedSeries(), CancellationToken.None);

        Assert.Empty(filteredFiles);
    }

    [Fact]
    public async Task EnsureTorrentAddedAsync_NotAddedAgain()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        services.QBitClient.GetTorrentsAsync(Arg.Any<TorrentListQuery?>(), Arg.Any<CancellationToken>())
            .Returns([new TorrentInfo {Hash = SpiceAndWolfHash}]);

        await services.QBitContentManager.EnsureTorrentAddedAsync(CreateDownloadRequestDto(), string.Empty,
            CancellationToken.None);

        await services.QBitClient.DidNotReceiveWithAnyArgs().AddTorrentsAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task EnsureTorrentAddedAsync_CalledWhenMissing()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        services.QBitClient.GetTorrentsAsync(Arg.Any<TorrentListQuery?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await services.QBitContentManager.EnsureTorrentAddedAsync(CreateDownloadRequestDto(), string.Empty, CancellationToken.None);

        await services.QBitClient.Received()
            .AddTorrentsAsync(Arg.Any<AddTorrentUrlsRequest>(), CancellationToken.None);
    }

    [Fact]
    public async Task SaveExternalDownloadRecord_MetadataIncludesOwnId()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        await QBitContentManager.SaveExternalDownloadRecord(unitOfWork, CreateDownloadRequestDto(), string.Empty, [], [], CancellationToken.None);

        var ed = await ctx.ExternalDownloads.FirstAsync();

        Assert.Equal(ed.Metadata.GetKey(RequestConstants.ExternalDownloadId), ed.Id);
    }

    #endregion

    #region ApplyTorrentFileFiltersAsync

    [Fact]
    public async Task ApplyTorrentFileFiltersAsync_NewDownload_OnlyRequestedFilesEnabled()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        services.QBitClient
            .GetTorrentContentsAsync(SpiceAndWolfHash, Arg.Any<CancellationToken>())
            .Returns([
                new TorrentContent
                {
                    Index = 0,
                    Name = "Vol1.cbz",
                    Priority = TorrentContentPriority.Minimal
                },
                new TorrentContent
                {
                    Index = 1,
                    Name = "Vol2.cbz",
                    Priority = TorrentContentPriority.Minimal
                },
                new TorrentContent
                {
                    Index = 2,
                    Name = "Vol3.cbz",
                    Priority = TorrentContentPriority.Minimal
                }
            ]);

        var allFiles = new List<QBitContentManager.ParsedTorrentFile>
        {
            new(new TorrentFile("Vol1.cbz", "Vol1.cbz", 0), null!),
            new(new TorrentFile("Vol2.cbz", "Vol2.cbz", 0), null!),
            new(new TorrentFile("Vol3.cbz", "Vol3.cbz", 0), null!)
        };

        var toDownload = new List<QBitContentManager.ParsedTorrentFile>
        {
            allFiles[0],
            allFiles[2]
        };

        await services.QBitContentManager.ApplyTorrentFileFiltersAsync(
            SpiceAndWolfHash,
            allFiles,
            toDownload,
            newDownload: true,
            CancellationToken.None);

        await services.QBitClient.Received()
            .SetFilePriorityAsync(
                SpiceAndWolfHash,
                Arg.Is<IEnumerable<int>>(x => x.SequenceEqual(new[] { 1})),
                TorrentContentPriority.Skip,
                CancellationToken.None);

        await services.QBitClient.DidNotReceive()
            .SetFilePriorityAsync(
                SpiceAndWolfHash,
                Arg.Any<IEnumerable<int>>(),
                TorrentContentPriority.Minimal,
                CancellationToken.None);
    }

    [Fact]
    public async Task ApplyTorrentFileFiltersAsync_ExistingDownload_PreservesNonSeriesSelections()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork, new ParserService());

        services.QBitClient
            .GetTorrentContentsAsync(SpiceAndWolfHash, Arg.Any<CancellationToken>())
            .Returns([
                new TorrentContent
                {
                    Index = 0,
                    Name = "Vol1.cbz",
                    Priority = TorrentContentPriority.Minimal
                },
                new TorrentContent
                {
                    Index = 1,
                    Name = "Vol2.cbz",
                    Priority = TorrentContentPriority.Minimal
                },
                new TorrentContent
                {
                    Index = 2,
                    Name = "Vol3.cbz",
                    Priority = TorrentContentPriority.Skip
                },
                new TorrentContent
                {
                    Index = 3,
                    Name = "Readme.txt",
                    Priority = TorrentContentPriority.Minimal
                }
            ]);

        var seriesFiles = new List<QBitContentManager.ParsedTorrentFile>
        {
            new(new TorrentFile("Vol1.cbz", "Vol1.cbz", 0), null!),
            new(new TorrentFile("Vol2.cbz", "Vol2.cbz", 0), null!),
            new(new TorrentFile("Vol3.cbz", "Vol3.cbz", 0), null!)
        };

        var wanted = new List<QBitContentManager.ParsedTorrentFile>
        {
            seriesFiles[2]
        };

        await services.QBitContentManager.ApplyTorrentFileFiltersAsync(
            SpiceAndWolfHash,
            seriesFiles,
            wanted,
            newDownload: false,
            CancellationToken.None);

        await services.QBitClient.Received()
            .SetFilePriorityAsync(
                SpiceAndWolfHash,
                Arg.Is<IEnumerable<int>>(x => x.SequenceEqual(new[] { 2 })),
                TorrentContentPriority.Minimal,
                CancellationToken.None);

        await services.QBitClient.Received()
            .SetFilePriorityAsync(
                SpiceAndWolfHash,
                Arg.Is<IEnumerable<int>>(x => x.SequenceEqual(new []{0, 1})),
                TorrentContentPriority.Skip,
                CancellationToken.None);
    }

    [Fact]
    public async Task ApplyTorrentFileFiltersAsync_DoesNothing_WhenAlreadyCorrect()
    {
        var (unitOfWork, _, _) = await CreateDatabase();
        var services = CreateServices(unitOfWork);

        services.QBitClient
            .GetTorrentContentsAsync(SpiceAndWolfHash, Arg.Any<CancellationToken>())
            .Returns([
                new TorrentContent
                {
                    Index = 0,
                    Name = "Vol1.cbz",
                    Priority = TorrentContentPriority.Minimal
                },
                new TorrentContent
                {
                    Index = 1,
                    Name = "Vol2.cbz",
                    Priority = TorrentContentPriority.Skip
                }
            ]);

        var seriesFiles = new List<QBitContentManager.ParsedTorrentFile>
        {
            new(new TorrentFile("Vol1.cbz", "Vol1.cbz", 0), null!),
            new(new TorrentFile("Vol2.cbz", "Vol2.cbz", 0), null!)
        };

        await services.QBitContentManager.ApplyTorrentFileFiltersAsync(
            SpiceAndWolfHash,
            seriesFiles,
            [seriesFiles[0]],
            true,
            CancellationToken.None);

        await services.QBitClient.DidNotReceiveWithAnyArgs()
            .SetFilePriorityAsync(default!, default!, default, default);
    }

    #endregion

}
