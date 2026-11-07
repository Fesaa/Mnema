using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Services;

[TestSubject(typeof(MonitoredSeriesService))]
public class MonitoredSeriesSyncTests
{
    [Fact]
    public void SyncChapters_MarksEveryChapterInRangeAvailable()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = new MonitoredSeriesService(
            Substitute.For<ILogger<MonitoredSeriesService>>(),
            Substitute.For<IScannerService>(),
            new ParserService(),
            Substitute.For<IDownloadService>(),
            Substitute.For<IMetadataResolver>(),
            new ApplicationConfiguration { BaseDir = "/base" },
            unitOfWork,
            Substitute.For<IServiceProvider>(),
            Substitute.For<IMessageService>(),
            Substitute.For<IConnectionService>(),
            Substitute.For<IMetadataService>(),
            Substitute.For<INamingService>(),
            Substitute.For<IChapterCombineService>(),
            new FileSystem());

        var series = new MonitoredSeries
        {
            Id = Guid.NewGuid(),
            Title = "Series",
            BaseDir = "Series",
            Chapters = [],
            UnMatchedChapters = [],
        };

        var upstream = Enumerable.Range(1, 10)
            .Select(i => new Chapter
            {
                Id = i.ToString(),
                Title = $"Chapter {i}",
                VolumeMarker = "1",
                ChapterMarker = i.ToString(),
            })
            .ToList();

        var onDisk = new List<OnDiskContent>
        {
            new()
            {
                Path = "/base/Series/Series Vol. 1.cbz",
                FileName = "Series Vol. 1.cbz",
                Volume = "1",
                Chapter = "1-10",
            },
        };

        service.SyncChapters(series, upstream, onDisk);

        Assert.Equal(10, series.Chapters.Count);
        Assert.All(series.Chapters, chapter =>
        {
            Assert.Equal(MonitoredChapterStatus.Available, chapter.Status);
            Assert.Equal("/Series/Series Vol. 1.cbz", chapter.FilePath);
        });
        Assert.Empty(series.UnMatchedChapters);
    }
}
