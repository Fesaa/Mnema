using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Internal;
using Mnema.Providers.Services;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Services;

[TestSubject(typeof(ChapterCombineService))]
public class ChapterCombineServiceTests : IDisposable
{
    private readonly string _baseDir;
    private readonly IFileSystem _fileSystem = new FileSystem();
    private readonly ParserService _parserService = new();
    private readonly ChapterCombineService _service;

    public ChapterCombineServiceTests()
    {
        _baseDir = Path.Join(Path.GetTempPath(), "mnema-combine-tests", Guid.NewGuid().ToString("N"));
        _fileSystem.Directory.CreateDirectory(_baseDir);

        var configuration = new ApplicationConfiguration
        {
            BaseDir = _baseDir,
            DownloadDir = Path.Join(_baseDir, "downloads"),
            PersistentStorage = _baseDir,
            Host = "localhost",
        };

        var metadataService = new MetadataService(
            Substitute.For<ILogger<MetadataService>>(),
            Substitute.For<IEpubMetadataService>(),
            _parserService,
            _fileSystem);

        _service = new ChapterCombineService(
            Substitute.For<ILogger<ChapterCombineService>>(),
            _fileSystem,
            _parserService,
            metadataService,
            new NamingService(Substitute.For<ILogger<NamingService>>(), configuration, _parserService),
            configuration);
    }

    public void Dispose()
    {
        if (_fileSystem.Directory.Exists(_baseDir))
        {
            _fileSystem.Directory.Delete(_baseDir, true);
        }
    }

    [Fact]
    public async Task Combine_WritesVolumeAndDeletesSources()
    {
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);
        var second = CreateChapter("chapter-2.cbz", "1", "2", 2);

        var result = await _service.Combine(CreateSeries(), [first, second]);

        Assert.Equal("/Test Series Vol. 1.cbz", result.FilePath);
        Assert.Equal("1", result.ComicInfo.Volume);
        Assert.Equal("1-2", result.ComicInfo.Number);

        var output = Path.Join(_baseDir, result.FilePath);
        Assert.True(_fileSystem.File.Exists(output));

        using var archive = ZipFile.OpenRead(output);
        Assert.Equal(new[] { "page 0001.jpg", "page 0002.jpg" },
            archive.Entries
                .Where(e => e.FullName.StartsWith("page "))
                .Select(e => e.FullName)
                .Order()
                .ToArray());
        Assert.Equal(new byte[] { 1 }, ReadEntry(archive, "page 0001.jpg"));
        Assert.Equal(new byte[] { 2 }, ReadEntry(archive, "page 0002.jpg"));

        Assert.False(_fileSystem.File.Exists(Path.Join(_baseDir, "chapter-1.cbz")));
        Assert.False(_fileSystem.File.Exists(Path.Join(_baseDir, "chapter-2.cbz")));
    }

    [Fact]
    public async Task Combine_OrdersPagesByChapter()
    {
        var second = CreateChapter("chapter-2.cbz", "1", "2", 2);
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);

        var result = await _service.Combine(CreateSeries(), [second, first]);

        using var archive = ZipFile.OpenRead(Path.Join(_baseDir, result.FilePath));
        Assert.Equal(new byte[] { 1 }, ReadEntry(archive, "page 0001.jpg"));
        Assert.Equal(new byte[] { 2 }, ReadEntry(archive, "page 0002.jpg"));
    }

    [Fact]
    public async Task Combine_ThrowsWhenVolumesDiffer()
    {
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);
        var second = CreateChapter("chapter-2.cbz", "2", "2", 2);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.Combine(CreateSeries(), [first, second]));
    }

    [Fact]
    public async Task Combine_ThrowsWhenLessThanTwo()
    {
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.Combine(CreateSeries(), [first]));
    }

    [Fact]
    public async Task Combine_ThrowsWhenOutputExists()
    {
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);
        var second = CreateChapter("chapter-2.cbz", "1", "2", 2);
        _fileSystem.File.WriteAllText(Path.Join(_baseDir, "Test Series Vol. 1.cbz"), string.Empty);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.Combine(CreateSeries(), [first, second]));
    }

    [Fact]
    public async Task Combine_ThrowsWhenSameFileSelectedTwice()
    {
        var first = CreateChapter("chapter-1.cbz", "1", "1", 1);
        var second = CreateChapter("chapter-1.cbz", "1", "2", 2);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.Combine(CreateSeries(), [first, second]));
    }

    [Fact]
    public async Task Combine_ThrowsWhenVolumeIsMissing()
    {
        var first = CreateChapter("chapter-1.cbz", string.Empty, "1", 1);
        var second = CreateChapter("chapter-2.cbz", string.Empty, "2", 2);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.Combine(CreateSeries(), [first, second]));
    }

    private MonitoredChapter CreateChapter(string fileName, string volume, string chapter, byte pageByte)
    {
        var path = Path.Join(_baseDir, fileName);
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
        }

        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var page = archive.CreateEntry("page 0001.jpg");
            using (var stream = page.Open())
            {
                stream.WriteByte(pageByte);
            }

            var comicInfo = new ComicInfo
            {
                Series = "Test Series",
                Volume = volume,
                Number = chapter,
            };

            var entry = archive.CreateEntry("ComicInfo.xml");
            using var entryStream = entry.Open();
            new XmlSerializer(typeof(ComicInfo)).Serialize(entryStream, comicInfo);
        }

        return new MonitoredChapter
        {
            Id = Guid.NewGuid(),
            ExternalId = Guid.NewGuid().ToString(),
            Title = $"Chapter {chapter}",
            Summary = string.Empty,
            Volume = volume,
            Chapter = chapter,
            FilePath = fileName,
            Status = MonitoredChapterStatus.Available,
            SortOrder = float.Parse(chapter, CultureInfo.InvariantCulture),
        };
    }

    private static MonitoredSeries CreateSeries()
    {
        return new MonitoredSeries
        {
            Id = Guid.NewGuid(),
            Title = "Test Series",
            NormalizedTitle = "test series",
            Summary = string.Empty,
            TitleOverride = "Test Series",
            BaseDir = "Series",
            Format = Format.Archive,
            Provider = Provider.Mangadex,
            Metadata = new MetadataBag(),
        };
    }

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);

        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);

        return memory.ToArray();
    }
}
