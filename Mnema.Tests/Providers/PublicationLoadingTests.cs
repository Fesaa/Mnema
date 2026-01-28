using System.IO.Abstractions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Mnema.Models.Publication;
using Mnema.Providers;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Providers;

[TestSubject(typeof(Publication))]
public class PublicationLoadingTests
{

    private DownloadRequestDto CreateRequest()
    {
        return new DownloadRequestDto
        {
            Provider = Provider.Mangadex,
            Id = "e3fa25bc-23eb-46c3-af17-36ec9200456e",
            BaseDir = "Manga",
            TempTitle = "Contract Sisters",
            Metadata = new MetadataBag(),
        };
    }

    private (Publication, IServiceScope) CreateSut(Provider provider, DownloadRequestDto request)
    {
        var col = new ServiceCollection();
        col.AddSingleton(new ApplicationConfiguration());
        col.AddScoped<ILogger<Publication>>(_ => Substitute.For<ILogger<Publication>>());
        col.AddScoped<IConnectionService>(_ => Substitute.For<IConnectionService>());
        col.AddScoped<IFileSystem>(_ => Substitute.For<IFileSystem>());
        col.AddScoped<IMessageService>(_ => Substitute.For<IMessageService>());
        col.AddKeyedScoped<IContentManager>(provider, (_, _) => Substitute.For<IPublicationManager>());
        col.AddKeyedScoped<IRepository>(provider, (_, _) => Substitute.For<IRepository>());
        col.AddScoped<IHttpClientFactory>(_ => Substitute.For<IHttpClientFactory>());
        col.AddScoped<ISettingsService>(_ => Substitute.For<ISettingsService>());
        col.AddScoped<IUnitOfWork>(_ => Substitute.For<IUnitOfWork>());
        col.AddScoped<IScannerService>(_ => Substitute.For<IScannerService>());
        col.AddScoped<IImageService>(_ => Substitute.For<IImageService>());
        col.AddScoped<IMetadataService>(_ => Substitute.For<IMetadataService>());
        col.AddScoped<INamingService>(_ => new NamingService(Substitute.For<ILogger<NamingService>>(), new ApplicationConfiguration()));

        var scope = col.BuildServiceProvider().CreateScope();

        var pub = new Publication(scope, provider, request)
        {
            Series = new Series
            {
                Id = "e3fa25bc-23eb-46c3-af17-36ec9200456e/",
                Title = "Contract Sisters",
                Summary = "",
                Status = PublicationStatus.Ongoing,
                Tags = [],
                People = [],
                Links = [],
                Chapters = []
            }
        };

        return (pub, scope);
    }

    [Fact]
    public void TestFindsVolumeFiles()
    {
        var (pub, scope) = CreateSut(Provider.Mangadex, CreateRequest());

        var scanner = scope.ServiceProvider.GetRequiredService<IScannerService>();

        scanner.ScanDirectory(
                Arg.Any<string>(),
                Arg.Any<ContentFormat>(),
                Arg.Any<Format>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs([
                new OnDiskContent
                {
                    Path = "Contract Sisters Vol. 1.cbz",
                    Volume = "1",
                    Chapter = string.Empty,
                }
            ]);

        pub.Series!.Chapters.Add(new Chapter
        {
            Id = "myId",
            Title = string.Empty,
            VolumeMarker = "1",
            ChapterMarker = "2",
            Tags = [],
            People = [],
            TranslationGroups = []
        });


        pub.FilterAlreadyDownloadedContent(CancellationToken.None);

        Assert.Empty(pub.QueuedChapters);
    }

    [Fact]
    public void TestVolumeChanges()
    {
        var (pub, scope) = CreateSut(Provider.Mangadex, CreateRequest());

        var scanner = scope.ServiceProvider.GetRequiredService<IScannerService>();

        scanner.ScanDirectory(
                Arg.Any<string>(),
                Arg.Any<ContentFormat>(),
                Arg.Any<Format>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs([
                new OnDiskContent
                {
                    Path = "Contract Sisters Ch. 5.cbz",
                    Volume = string.Empty,
                    Chapter = "5",
                },
                new OnDiskContent
                {
                    Path = "Contract Sisters Vol. 1 Ch. 4.cbz",
                    Volume = "1",
                    Chapter = "4",
                }
            ]);

        pub.Series!.Chapters.Add(new Chapter
        {
            Id = "myId",
            Title = string.Empty,
            VolumeMarker = "1",
            ChapterMarker = "5",
            Tags = [],
            People = [],
            TranslationGroups = []
        });
        pub.Series!.Chapters.Add(new Chapter
        {
            Id = "myI2",
            Title = string.Empty,
            VolumeMarker = "1",
            ChapterMarker = "4",
            Tags = [],
            People = [],
            TranslationGroups = []
        });

        pub.FilterAlreadyDownloadedContent(CancellationToken.None);

        Assert.Single(pub.QueuedChapters);
        Assert.Single(pub.ToRemovePaths);
    }

}
