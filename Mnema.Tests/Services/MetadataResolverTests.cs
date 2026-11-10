using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using Mnema.Services;
using NSubstitute;
using Xunit.Abstractions;

namespace Mnema.Tests.Services;

[TestSubject(typeof(MetadataResolver))]
public class MetadataResolverTests(ITestOutputHelper testOutputHelper) : DatabaseTests(testOutputHelper)
{

    private static Task<Series?> ResolveSeriesAsync(
        IUnitOfWork unitOfWork,
        Series? hardCoverSeries,
        Series? mangabakaSeries,
        bool enrichWithCovers = false,
        List<Cover>? hardCoverCovers = null,
        List<Cover>? mangabakaCovers = null,
        Series? upstreamSeries = null,
        CancellationToken ct = default
        )
    {
        var metadataProviderService = Substitute.For<IMetadataProviderService>();
        metadataProviderService.GetSeries("1", CancellationToken.None)
            .Returns(hardCoverSeries);
        metadataProviderService.GetSeries("2", CancellationToken.None)
            .Returns(mangabakaSeries);
        metadataProviderService.GetCovers("1", CancellationToken.None)
            .Returns(hardCoverCovers ?? []);
        metadataProviderService.GetCovers("2", CancellationToken.None)
            .Returns(mangabakaCovers ?? []);

        var metadata = new MetadataBag();
        metadata.SetValue(RequestConstants.HardcoverSeriesIdKey.Key, "1");
        metadata.SetValue(RequestConstants.MangaBakaKey.Key, "2");

        if (enrichWithCovers)
        {
            metadata.SetKey(MetadataResolverOptions.EnrichWithCovers, true);
        }

        var services = new ServiceCollection();
        if (upstreamSeries != null)
        {
            var upstreamRepository = Substitute.For<IRepository>();
            upstreamRepository.SeriesInfo(Arg.Any<DownloadRequestDto>(), Arg.Any<CancellationToken>())
                .Returns(upstreamSeries);

            services.AddKeyedSingleton<IRepository>(Provider.Nyaa, upstreamRepository);
            metadata.SetKey(RequestConstants.ExternalIdKey, "upstream-1");
        }

        return new MetadataResolver(
            new ParserService(),
            metadataProviderService,
            metadataProviderService,
            services.BuildServiceProvider(),
            unitOfWork
        ).ResolveSeriesAsync(Provider.Nyaa, metadata, ct);
    }

    private static MetadataProviderSettings CreateSettings(
        bool enabled = true,
        int priority = 1,
        bool title = true,
        bool summary = true,
        bool localizedSeries = true,
        bool coverUrl = true,
        bool publicationStatus = true,
        bool year = true,
        bool ageRating = true,
        bool tags = true,
        bool people = true,
        bool links = true,
        bool chapters = true,
        MetadataProvider metadataProvider = MetadataProvider.Hardcover)
    {
        return new MetadataProviderSettings
        {
            MetadataProvider = metadataProvider,
            Enabled = enabled,
            Priority = priority,
            SeriesTitle = title,
            SeriesSummary = summary,
            SeriesLocalizedName = localizedSeries,
            SeriesCoverUrl = coverUrl,
            SeriesPublicationStatus = publicationStatus,
            SeriesYear = year,
            SeriesAgeRating = ageRating,
            SeriesTags = tags,
            SeriesPeople = people,
            SeriesLinks = links,
            Chapters = chapters,
            ChapterCoverUrl = true,
            ChapterTags = true,
            ChapterPeople = true,
            ChapterReleaseDate = true,
            ChapterSummary = true,
            ChapterTitle = true,
        };
    }

    [Fact]
    public async Task ResolveSeriesAsync_BothSeriesNull_ReturnsNull()
    {
        var (unitOfWork, _, _) = await CreateDatabase();

        var result = await ResolveSeriesAsync(unitOfWork, null, null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveSeriesAsync_OnlyHardCoverSeries_ReturnsHardCoverSeries()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings());
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Test Series",
            Summary = "Test Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null);

        Assert.NotNull(result);
        Assert.Equal("Test Series", result.Title);
    }

    [Fact]
    public async Task ResolveSeriesAsync_OnlyMangabakaSeries_ReturnsMangabakaSeries()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Mangabaka Series",
            Summary = "Mangabaka Summary",
            Status = PublicationStatus.Completed,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, null, mangabaka);

        Assert.NotNull(result);
        Assert.Equal("Mangabaka Series", result.Title);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesNullTitle_FromSecondSource()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "",
            Summary = "HardCover Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Mangabaka Title",
            Summary = "Mangabaka Summary",
            Status = PublicationStatus.Completed,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal("Mangabaka Title", result.Title);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DoesNotMergeTitle_WhenDisabled()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka, title: false));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "",
            Summary = "HardCover Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Mangabaka Title",
            Summary = "Mangabaka Summary",
            Status = PublicationStatus.Completed,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal("", result.Title);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesPeople_DistinctByName()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>
            {
                Person.Create("Author A", PersonRole.Writer),
                Person.Create("Artist B", PersonRole.Colorist)
            },
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>
            {
                Person.Create("Author A", PersonRole.Writer), // Duplicate
                Person.Create("Artist C", PersonRole.Translator) // New
            },
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(3, result.People.Count);
        Assert.Contains(result.People, p => p.Name == "Author A");
        Assert.Contains(result.People, p => p.Name == "Artist B");
        Assert.Contains(result.People, p => p.Name == "Artist C");
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesTags_Distinct()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();
        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>
            {
                new Tag("Action", true),
                new Tag("Adventure", false)
            },
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>
            {
                new Tag("Action", true), // Duplicate
                new Tag("Fantasy", true) // New
            },
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Value == "Action");
        Assert.Contains(result.Tags, t => t.Value == "Adventure");
        Assert.Contains(result.Tags, t => t.Value == "Fantasy");
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesLinks_Distinct()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string> { "https://example.com/1", "https://example.com/2" },
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string> { "https://example.com/2", "https://example.com/3" },
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(3, result.Links.Count);
        Assert.Contains("https://example.com/1", result.Links);
        Assert.Contains("https://example.com/2", result.Links);
        Assert.Contains("https://example.com/3", result.Links);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesChapters_Distinct()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var chapter1 = new Chapter
        {
            Id = "ch1",
            Title = "Chapter 1",
            VolumeMarker = "1",
            ChapterMarker = "1",
            Tags = new List<Tag>(),
            People = new List<Person>(),
            TranslationGroups = new List<string>()
        };

        var chapter2 = new Chapter
        {
            Id = "ch2",
            Title = "Chapter 2",
            VolumeMarker = "1",
            ChapterMarker = "2",
            Tags = new List<Tag>(),
            People = new List<Person>(),
            TranslationGroups = new List<string>()
        };

        var chapter3 = new Chapter
        {
            Id = "ch3",
            Title = "Chapter 3",
            VolumeMarker = "1",
            ChapterMarker = "3",
            Tags = new List<Tag>(),
            People = new List<Person>(),
            TranslationGroups = new List<string>()
        };

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter> { chapter1, chapter2 }
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter> { chapter2, chapter3 }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(3, result.Chapters.Count);
    }

    [Fact]
    public async Task ResolveSeriesAsync_PreservesNonNullValues_WhenMerging()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "HardCover Title",
            Summary = "HardCover Summary",
            LocalizedSeries = "Localized",
            CoverUrl = "https://cover.com/1",
            Status = PublicationStatus.Ongoing,
            Year = 2020,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Mangabaka Title",
            Summary = "Mangabaka Summary",
            LocalizedSeries = "Different Localized",
            CoverUrl = "https://cover.com/2",
            Status = PublicationStatus.Completed,
            Year = 2021,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal("HardCover Title", result.Title);
        Assert.Equal("HardCover Summary", result.Summary);
        Assert.Equal("Localized", result.LocalizedSeries);
        Assert.Equal("https://cover.com/1", result.CoverUrl);
        Assert.Equal(PublicationStatus.Ongoing, result.Status);
        Assert.Equal(2020, result.Year);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesNullYear_FromSecondSource()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Year = null,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            Year = 2023,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(2023, result.Year);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesAgeRating_WhenNull()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            AgeRating = null,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "Summary",
            Status = PublicationStatus.Ongoing,
            AgeRating = AgeRating.Mature,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(AgeRating.Mature, result.AgeRating);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DisabledProvider_DoesNotMerge()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka, enabled: false));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "HardCover",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Mangabaka",
            Summary = "Should not merge",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal("", result.Summary);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesVolumeOnlyChapters()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "hc-vol-1",
                    Title = "HardCover Volume 1",
                    VolumeMarker = "1",
                    ChapterMarker = "",
                    Isbn = "9780000000001",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "mb-vol-1",
                    Title = "Mangabaka Volume 1",
                    VolumeMarker = "1",
                    ChapterMarker = "",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        var volume = Assert.Single(result.Chapters);
        Assert.Equal("hc-vol-1", volume.Id);
        Assert.Equal("HardCover Volume 1", volume.Title);
        Assert.Equal("9780000000001", volume.Isbn);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DoesNotMergeAgeRating_WhenDisabled()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka, ageRating: false));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            AgeRating = AgeRating.Unknown,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            AgeRating = AgeRating.Mature,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(AgeRating.Unknown, result.AgeRating);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesHighestCounts_WhenStatusIsAlreadySet()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var mangabaka = new Series
        {
            Id = "mb1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Completed,
            HighestVolumeNumber = 10,
            HighestChapterNumber = 100,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, mangabaka);

        Assert.NotNull(result);
        Assert.Equal(PublicationStatus.Ongoing, result.Status);
        Assert.Equal(10, result.HighestVolumeNumber);
        Assert.Equal(100, result.HighestChapterNumber);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesCoverUrlAndNonProxiedCoverUrlTogether()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            CoverUrl = "https://cover.example/1.jpg",
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null);

        Assert.NotNull(result);
        Assert.Equal("https://cover.example/1.jpg", result.CoverUrl);
        Assert.Equal("https://cover.example/1.jpg", result.NonProxiedCoverUrl);
    }

    [Fact]
    public async Task ResolveSeriesAsync_MergesOriginalLanguage()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            OriginalLanguage = "ja",
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null);

        Assert.NotNull(result);
        Assert.Equal("ja", result.OriginalLanguage);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DoesNotOverwriteExistingChapterCover_WithEnrichedCovers()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "ch1",
                    Title = "Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    CoverUrl = "https://provider.example/1.jpg",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var mangabakaCovers = new List<Cover>
        {
            new() { Url = "https://mangabaka.example/1.jpg", Extension = "jpg", Volume = "1" }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null,
            enrichWithCovers: true, mangabakaCovers: mangabakaCovers);

        Assert.NotNull(result);
        var chapter = Assert.Single(result.Chapters);
        Assert.Equal("https://provider.example/1.jpg", chapter.CoverUrl);
    }

    [Fact]
    public async Task ResolveSeriesAsync_AppliesEnrichedCovers_InPriorityOrder()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Mangabaka));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hc1",
            Title = "Series",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "ch1",
                    Title = "Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var hardCoverCovers = new List<Cover>
        {
            new() { Url = "https://hardcover.example/1.jpg", Extension = "jpg", Volume = "1" }
        };

        var mangabakaCovers = new List<Cover>
        {
            new() { Url = "https://mangabaka.example/1.jpg", Extension = "jpg", Volume = "1" }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null,
            enrichWithCovers: true, hardCoverCovers: hardCoverCovers, mangabakaCovers: mangabakaCovers);

        Assert.NotNull(result);
        var chapter = Assert.Single(result.Chapters);
        Assert.Equal("https://hardcover.example/1.jpg", chapter.CoverUrl);
        Assert.Equal(".jpg", chapter.CoverFileFormat);
    }

    [Fact]
    public async Task ResolveSeriesAsync_UpstreamOverridesChapterIdentity()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Upstream));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hardcover-series",
            Title = "Spice and Wolf (Hardcover)",
            Summary = "Hardcover summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "hardcover-ch1",
                    Title = "Spice and Wolf Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    RefUrl = "https://hardcover.example/ch1",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var upstream = new Series
        {
            Id = "upstream-series",
            Title = "Spice and Wolf (Upstream)",
            Summary = "Upstream summary",
            OriginalLanguage = "ja",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "upstream-ch1",
                    Title = "Spice and Wolf Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    RefUrl = "https://provider.example/ch1",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null, upstreamSeries: upstream);

        Assert.NotNull(result);
        Assert.Equal("upstream-series", result.Id);
        Assert.Equal("Spice and Wolf (Hardcover)", result.Title);
        Assert.Equal("ja", result.OriginalLanguage);

        var chapter = Assert.Single(result.Chapters);
        Assert.Equal("upstream-ch1", chapter.Id);
        Assert.Equal("https://provider.example/ch1", chapter.RefUrl);
        Assert.Equal(MetadataProvider.Upstream, chapter.MetadataProvider);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DisabledUpstream_StillAttachesChapters()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Upstream, enabled: false));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hardcover-series",
            Title = "Spice and Wolf",
            Summary = "Hardcover summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>()
        };

        var upstream = new Series
        {
            Id = "upstream-series",
            Title = "Spice and Wolf (Upstream)",
            Summary = "Upstream summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "upstream-ch2",
                    Title = "Spice and Wolf Chapter 2",
                    VolumeMarker = "2",
                    ChapterMarker = "3",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null, upstreamSeries: upstream);

        Assert.NotNull(result);
        Assert.Equal("upstream-series", result.Id);
        Assert.Equal("Spice and Wolf", result.Title);

        var chapter = Assert.Single(result.Chapters);
        Assert.Equal("upstream-ch2", chapter.Id);
        Assert.Equal(MetadataProvider.Upstream, chapter.MetadataProvider);
    }

    [Fact]
    public async Task ResolveSeriesAsync_DisabledUpstream_ProvidesTitleFallback()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Upstream, enabled: false));
        await ctx.SaveChangesAsync();

        var upstream = new Series
        {
            Id = "upstream-series",
            Title = "Spice and Wolf",
            Summary = "Upstream summary",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "upstream-ch1",
                    Title = "Spice and Wolf Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var result = await ResolveSeriesAsync(unitOfWork, null, null, upstreamSeries: upstream);

        Assert.NotNull(result);
        Assert.Equal("Spice and Wolf", result.Title);
        Assert.Single(result.Chapters);
    }

    [Fact]
    public async Task ResolveSeriesAsync_KeepsMetadataOnlyChaptersMarked()
    {
        var (unitOfWork, ctx, _) = await CreateDatabase();

        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 1));
        ctx.MetadataProviderSettings.Add(CreateSettings(priority: 2, metadataProvider: MetadataProvider.Upstream));
        await ctx.SaveChangesAsync();

        var hardCover = new Series
        {
            Id = "hardcover-series",
            Title = "Spice and Wolf",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "hardcover-volume-1",
                    Title = "Spice and Wolf, Vol. 1",
                    VolumeMarker = "1",
                    ChapterMarker = "",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var upstream = new Series
        {
            Id = "upstream-series",
            Title = "Spice and Wolf",
            Summary = "",
            Status = PublicationStatus.Ongoing,
            Tags = new List<Tag>(),
            People = new List<Person>(),
            Links = new List<string>(),
            Chapters = new List<Chapter>
            {
                new()
                {
                    Id = "upstream-ch1",
                    Title = "Spice and Wolf Chapter 1",
                    VolumeMarker = "1",
                    ChapterMarker = "1",
                    Tags = new List<Tag>(),
                    People = new List<Person>(),
                    TranslationGroups = new List<string>()
                }
            }
        };

        var result = await ResolveSeriesAsync(unitOfWork, hardCover, null, upstreamSeries: upstream);

        Assert.NotNull(result);
        Assert.Equal(2, result.Chapters.Count);

        var volume = Assert.Single(result.Chapters.Where(c => c.MetadataProvider == MetadataProvider.Hardcover));
        Assert.Equal("hardcover-volume-1", volume.Id);

        var chapter = Assert.Single(result.Chapters.Where(c => c.MetadataProvider == MetadataProvider.Upstream));
        Assert.Equal("upstream-ch1", chapter.Id);
    }
}
