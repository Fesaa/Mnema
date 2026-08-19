using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
        CancellationToken ct = default
        )
    {
        var metadataProviderService = Substitute.For<IMetadataProviderService>();
        metadataProviderService.GetSeries("1", CancellationToken.None)
            .Returns(hardCoverSeries);
        metadataProviderService.GetSeries("2", CancellationToken.None)
            .Returns(mangabakaSeries);

        var metadata = new MetadataBag();
        metadata.SetValue(RequestConstants.HardcoverSeriesIdKey.Key, "1");
        metadata.SetValue(RequestConstants.MangaBakaKey.Key, "2");

        return new MetadataResolver(
            Substitute.For<IParserService>(),
            metadataProviderService,
            metadataProviderService,
            Substitute.For<IServiceProvider>(),
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
                Person.Create("Artist B", PersonRole.Penciller)
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
                Person.Create("Artist C", PersonRole.CoverArtist) // New
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
}
