using System;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.Metadata;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Internal;
using Mnema.Providers.Services;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Providers.Services;

public class ScannerServiceTest: IDisposable
{
    private readonly string _baseDir = Path.Join(Path.GetTempPath(), "mnema-scanner-" + Guid.NewGuid());

    public ScannerServiceTest()
    {
        Directory.CreateDirectory(_baseDir);
    }

    [Fact]
    public void ScanDirectory_FindsFilesInSubdirectories()
    {
        var seriesDir = Path.Join(_baseDir, "Series");
        var volumeDir = Path.Join(seriesDir, "Vol. 1");
        Directory.CreateDirectory(volumeDir);

        File.WriteAllText(Path.Join(seriesDir, "Vol. 2.cbz"), string.Empty);
        File.WriteAllText(Path.Join(volumeDir, "Vol. 1.cbz"), string.Empty);

        var scanner = CreateScanner();

        var contents = scanner.ScanDirectory("Series", ContentFormat.Manga, Format.Archive, CancellationToken.None);

        Assert.Equal(2, contents.Count);
    }

    private ScannerService CreateScanner()
    {
        var metadataService = Substitute.For<IMetadataService>();
        metadataService.ParseComicInfoFromFile(Arg.Any<string>()).Returns((ComicInfo?)null);

        return new ScannerService(
            Substitute.For<ILogger<ScannerService>>(),
            new FileSystem(),
            new ParserService(),
            new ApplicationConfiguration { BaseDir = _baseDir },
            Substitute.For<INamingService>(),
            new HttpClient(),
            Substitute.For<IDistributedCache>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IMessageService>(),
            Substitute.For<IMangabakaMetadataService>(),
            metadataService
        );
    }

    public void Dispose()
    {
        if (Directory.Exists(_baseDir))
        {
            Directory.Delete(_baseDir, true);
        }

        GC.SuppressFinalize(this);
    }
}
