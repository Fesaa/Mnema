using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Services;
using NSubstitute;

namespace Mnema.Tests.Services;

[TestSubject(typeof(UrlParserService))]
public class UrlParserServiceTests
{
    private const string MangadexId = "0f9d2f79-2c14-4a8a-9f0b-123456789abc";

    [Fact]
    public async Task ParseUrl_MangadexUrl_ReturnsMangadexId()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl($"https://mangadex.org/title/{MangadexId}/some-slug");

        Assert.NotNull(result);
        Assert.Equal(Provider.Mangadex, result.Provider);
        Assert.Null(result.MetadataProvider);
        Assert.Equal(MangadexId, result.ExternalId);
        Assert.Equal("content summary", result.Description);
    }

    [Fact]
    public async Task ParseUrl_MangadexUrlWithoutScheme_ReturnsMangadexId()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl($"mangadex.org/title/{MangadexId}");

        Assert.NotNull(result);
        Assert.Equal(Provider.Mangadex, result.Provider);
        Assert.Equal(MangadexId, result.ExternalId);
    }

    [Fact]
    public async Task ParseUrl_DynastyUrl_ReturnsSeriesPath()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl("https://dynasty-scans.com/series/gal_and_gal_yuri");

        Assert.NotNull(result);
        Assert.Equal(Provider.Dynasty, result.Provider);
        Assert.Equal("/series/gal_and_gal_yuri", result.ExternalId);
    }

    [Fact]
    public async Task ParseUrl_WebtoonsListUrl_ReturnsListPath()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl("https://www.webtoons.com/en/fantasy/tower-of-god/list?title_no=95");

        Assert.NotNull(result);
        Assert.Equal(Provider.Webtoons, result.Provider);
        Assert.Equal("en/fantasy/tower-of-god/list?title_no=95", result.ExternalId);
    }

    [Fact]
    public async Task ParseUrl_WebtoonsEpisodeUrl_RewritesToListPath()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl(
            "https://www.webtoons.com/en/fantasy/tower-of-god/episode-1/viewer?title_no=95&episode_no=1");

        Assert.NotNull(result);
        Assert.Equal(Provider.Webtoons, result.Provider);
        Assert.Equal("en/fantasy/tower-of-god/list?title_no=95", result.ExternalId);
    }

    [Theory]
    [InlineData("https://mangabaka.org/1995")]
    [InlineData("https://mangabaka.org/manga/1995/Frieren-Beyond-Journey-s-End")]
    public async Task ParseUrl_MangabakaUrl_ReturnsMangabakaId(string url)
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl(url);

        Assert.NotNull(result);
        Assert.Null(result.Provider);
        Assert.Equal(MetadataProvider.Mangabaka, result.MetadataProvider);
        Assert.Equal("1995", result.ExternalId);
    }

    [Theory]
    [InlineData("https://hardcover.app/id/series/1033", "1033")]
    [InlineData("https://hardcover.app/series/1092/edit", "1092")]
    public async Task ParseUrl_HardcoverIdUrl_ReturnsHardcoverId(string url, string expectedId)
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl(url);

        Assert.NotNull(result);
        Assert.Null(result.Provider);
        Assert.Equal(MetadataProvider.Hardcover, result.MetadataProvider);
        Assert.Equal(expectedId, result.ExternalId);
    }

    [Fact]
    public async Task ParseUrl_HardcoverSlugUrl_ReturnsCanonicalId()
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl("https://hardcover.app/series/red-rising-saga");

        Assert.NotNull(result);
        Assert.Equal(MetadataProvider.Hardcover, result.MetadataProvider);
        Assert.Equal("1033", result.ExternalId);
        Assert.Equal("metadata summary", result.Description);
    }

    [Theory]
    [InlineData("https://example.com/series/123")]
    [InlineData("https://hardcover.app/books/the-way-of-kings")]
    [InlineData("https://mangadex.org/chapter/0f9d2f79-2c14-4a8a-9f0b-123456789abc")]
    [InlineData("")]
    [InlineData("not a url")]
    public async Task ParseUrl_UnsupportedUrl_ReturnsNull(string url)
    {
        var (service, _, _) = CreateService();

        var result = await service.ParseUrl(url);

        Assert.Null(result);
    }

    private static (UrlParserService Service, IRepository Repository, IMetadataProviderService MetadataProvider)
        CreateService()
    {
        var repository = Substitute.For<IRepository>();
        repository.SeriesInfo(Arg.Any<DownloadRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<DownloadRequestDto>();
                return new Series
                {
                    Id = request.Id,
                    Title = "content series",
                    Summary = "content summary",
                    Status = PublicationStatus.Ongoing,
                    Tags = [],
                    People = [],
                    Links = [],
                    Chapters = [],
                };
            });

        var metadataProvider = Substitute.For<IMetadataProviderService>();
        metadataProvider.GetSeries(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.Arg<string>();
                return new Series
                {
                    Id = id == "red-rising-saga" ? "1033" : id,
                    Title = "metadata series",
                    Summary = "metadata summary",
                    Status = PublicationStatus.Ongoing,
                    Tags = [],
                    People = [],
                    Links = [],
                    Chapters = [],
                };
            });

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IRepository>(Provider.Mangadex, repository);
        services.AddKeyedSingleton<IRepository>(Provider.Dynasty, repository);
        services.AddKeyedSingleton<IRepository>(Provider.Webtoons, repository);
        services.AddKeyedSingleton<IMetadataProviderService>(MetadataProvider.Hardcover, metadataProvider);
        services.AddKeyedSingleton<IMetadataProviderService>(MetadataProvider.Mangabaka, metadataProvider);

        return (new UrlParserService(services.BuildServiceProvider()), repository, metadataProvider);
    }
}
