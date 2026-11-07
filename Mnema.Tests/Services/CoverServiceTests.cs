using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Services;

[TestSubject(typeof(CoverService))]
public class CoverServiceTests : IDisposable
{
    private readonly string _baseDir;
    private readonly IFileSystem _fileSystem = new FileSystem();
    private readonly ParserService _parserService = new();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();

    public CoverServiceTests()
    {
        _baseDir = Path.Join(Path.GetTempPath(), "mnema-cover-tests", Guid.NewGuid().ToString("N"));
        _fileSystem.Directory.CreateDirectory(_baseDir);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
    }

    public void Dispose()
    {
        if (_fileSystem.Directory.Exists(_baseDir))
        {
            _fileSystem.Directory.Delete(_baseDir, true);
        }
    }

    [Fact]
    public async Task GetArchiveCover_PrefersMnemaCover()
    {
        CreateArchive("chapter.cbz",
            ("ComicInfo.xml", [1, 2, 3]),
            ("cover.jpg", [4, 5, 6]),
            ("!0000 cover.png", [7, 8, 9]));

        var service = CreateService();
        var cover = await service.GetArchiveCover(CreateChapter("chapter.cbz"));

        Assert.NotNull(cover);
        Assert.Equal(".png", cover.Extension);
        Assert.Equal(new byte[] { 7, 8, 9 }, cover.Data);
    }

    [Fact]
    public async Task GetArchiveCover_FallsBackToParserCover()
    {
        CreateArchive("chapter.cbz",
            ("page 0001.jpg", [1, 2, 3]),
            ("cover.jpg", [4, 5, 6]));

        var service = CreateService();
        var cover = await service.GetArchiveCover(CreateChapter("chapter.cbz"));

        Assert.NotNull(cover);
        Assert.Equal(".jpg", cover.Extension);
        Assert.Equal(new byte[] { 4, 5, 6 }, cover.Data);
    }

    [Fact]
    public async Task GetArchiveCover_IgnoresBackCover()
    {
        CreateArchive("chapter.cbz", ("back cover.jpg", [4, 5, 6]));

        var service = CreateService();
        var cover = await service.GetArchiveCover(CreateChapter("chapter.cbz"));

        Assert.Null(cover);
    }

    [Fact]
    public async Task UpdateCovers_ReplacesExistingCoverAndKeepsOtherEntries()
    {
        CreateArchive("chapter.cbz",
            ("ComicInfo.xml", [1, 2, 3]),
            ("page 0001.jpg", [4, 5, 6]),
            ("cover.jpg", [7, 8, 9]));

        var cover = new Cover
        {
            Url = "https://example.com/cover.jpg",
            Extension = "jpg",
            Volume = "1",
        };

        var repository = Substitute.For<IRepository, ICoverRepository>();
        ((ICoverRepository)repository).GetCovers(Arg.Any<DownloadRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<Cover> { cover }));

        var newCover = new byte[] { 10, 11, 12 };
        var service = CreateService(repository, newCover);
        var chapter = CreateChapter("chapter.cbz");

        await service.UpdateCovers(CreateSeries(), [chapter], cover.Url);

        using var stream = _fileSystem.File.OpenRead(Path.Join(_baseDir, "chapter.cbz"));
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Null(archive.GetEntry("cover.jpg"));
        Assert.Equal(new byte[] { 1, 2, 3 }, ReadEntry(archive, "ComicInfo.xml"));
        Assert.Equal(new byte[] { 4, 5, 6 }, ReadEntry(archive, "page 0001.jpg"));
        Assert.Equal(newCover, ReadEntry(archive, "!0000 cover.jpg"));
        Assert.NotEqual(default, chapter.LastModifiedUtc);
    }

    [Fact]
    public void CoverDto_IsMapped()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(AutoMapperProfiles).Assembly),
            Substitute.For<ILoggerFactory>());

        var dto = configuration.CreateMapper().Map<CoverDto>(new Cover
        {
            Url = "https://example.com/cover.jpg",
            Extension = "jpg",
            Volume = "1",
        });

        Assert.Equal("https://example.com/cover.jpg", dto.Url);
        Assert.Equal("jpg", dto.Extension);
        Assert.Equal("1", dto.Volume);
    }

    [Fact]
    public async Task UpdateCovers_RejectsMixedVolumes()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.UpdateCovers(CreateSeries(),
                [CreateChapter("a.cbz", "1"), CreateChapter("b.cbz", "2")],
                "https://example.com/cover.jpg"));

        Assert.Contains("single volume", exception.Message);
    }

    private CoverService CreateService(IRepository? repository = null, byte[]? coverData = null)
    {
        if (coverData != null)
        {
            _httpClientFactory.CreateClient(nameof(Provider.Mangadex))
                .Returns(new HttpClient(new StubHttpMessageHandler(coverData)));
        }

        var services = new StubKeyedServiceProvider();
        if (repository != null)
        {
            services.Add(typeof(IRepository), Provider.Mangadex, repository);
        }

        return new CoverService(
            Substitute.For<ILogger<CoverService>>(),
            services,
            _httpClientFactory,
            _fileSystem,
            _parserService,
            _unitOfWork,
            new ApplicationConfiguration
            {
                BaseDir = _baseDir,
                DownloadDir = _baseDir,
                PersistentStorage = _baseDir,
                Host = "http://localhost",
            });
    }

    private string CreateArchive(string name, params (string Name, byte[] Data)[] entries)
    {
        var path = Path.Join(_baseDir, name);
        using var stream = _fileSystem.File.Create(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        foreach (var (entryName, data) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            entryStream.Write(data);
        }

        return path;
    }

    private static MonitoredSeries CreateSeries() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Series",
        BaseDir = string.Empty,
        Provider = Provider.Mangadex,
        ExternalId = "series-id",
        Format = Format.Archive,
        Metadata = new MetadataBag(),
        Chapters = [],
    };

    private static MonitoredChapter CreateChapter(string filePath, string volume = "1") => new()
    {
        Id = Guid.NewGuid(),
        ExternalId = "chapter-id",
        Title = "Chapter",
        Volume = volume,
        Chapter = "1",
        FilePath = filePath,
    };

    private static byte[] ReadEntry(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        Assert.NotNull(entry);

        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private sealed class StubHttpMessageHandler(byte[] data) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(data),
            });
        }
    }

    private sealed class StubKeyedServiceProvider : IServiceProvider, IKeyedServiceProvider
    {
        private readonly Dictionary<(Type, object?), object> _services = [];

        public void Add(Type type, object key, object service) => _services[(type, key)] = service;

        public object? GetService(Type serviceType) => null;

        public object? GetKeyedService(Type serviceType, object? serviceKey)
            => _services.GetValueOrDefault((serviceType, serviceKey));

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
            => GetKeyedService(serviceType, serviceKey)
               ?? throw new InvalidOperationException($"No service for {serviceType}");
    }
}
