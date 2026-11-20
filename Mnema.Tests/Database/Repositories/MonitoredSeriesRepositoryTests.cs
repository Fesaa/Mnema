using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Xunit.Abstractions;

namespace Mnema.Tests.Database.Repositories;

public class MonitoredSeriesRepositoryTests(ITestOutputHelper testOutputHelper) : DatabaseTests(testOutputHelper)
{
    [Fact]
    public async Task LookupMonitoredSeries_MatchesEveryQueryWord()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        context.MonitoredSeries.AddRange(
            CreateSeries("Spice and Wolf"),
            CreateSeries("Wolf Children"),
            CreateSeries("Fullmetal Alchemist"));
        await context.SaveChangesAsync();

        var results = await unitOfWork.MonitoredSeriesRepository.LookupMonitoredSeries(
            "spice wolf", 5, CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Spice and Wolf", result.Title);
    }

    [Fact]
    public async Task LookupMonitoredSeries_EmptyQuery_ReturnsEmpty()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        context.MonitoredSeries.Add(CreateSeries("Spice and Wolf"));
        await context.SaveChangesAsync();

        var results = await unitOfWork.MonitoredSeriesRepository.LookupMonitoredSeries(
            "   ", 5, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task LookupMonitoredSeries_RespectsLimitAndOrder()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        context.MonitoredSeries.AddRange(
            CreateSeries("Spice Girls"),
            CreateSeries("Spice and Wolf"),
            CreateSeries("Spice and Wolf II"));
        await context.SaveChangesAsync();

        var results = await unitOfWork.MonitoredSeriesRepository.LookupMonitoredSeries(
            "spice", 2, CancellationToken.None);

        Assert.Equal(new[] { "Spice and Wolf", "Spice and Wolf II" }, results.Select(r => r.Title));
    }

    [Fact]
    public async Task LookupMonitoredSeries_ProjectsLookupFields()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        var series = CreateSeries("Spice and Wolf",
            coverUrl: "https://example.com/cover.jpg",
            provider: Provider.Mangadex);
        context.MonitoredSeries.Add(series);
        await context.SaveChangesAsync();

        var result = Assert.Single(await unitOfWork.MonitoredSeriesRepository.LookupMonitoredSeries(
            "spice", 5, CancellationToken.None));

        Assert.Equal(series.Id, result.Id);
        Assert.Equal("Spice and Wolf", result.Title);
        Assert.Equal("https://example.com/cover.jpg", result.CoverUrl);
        Assert.Equal(Provider.Mangadex, result.Provider);
    }

    [Fact]
    public async Task GetMonitoredSeriesDtosForUser_MatchesEveryQueryWord()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        context.MonitoredSeries.AddRange(
            CreateSeries("Spice and Wolf"),
            CreateSeries("Wolf Children"),
            CreateSeries("Fullmetal Alchemist"));
        await context.SaveChangesAsync();

        var results = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDtosForUser(
            "spice wolf", null, PaginationParams.Default, CancellationToken.None);

        var result = Assert.Single(results.Items);
        Assert.Equal("Spice and Wolf", result.Title);
    }

    [Fact]
    public async Task GetMonitoredSeriesDtosForUser_EmptyQuery_ReturnsAll()
    {
        var (unitOfWork, context, _) = await CreateDatabase();

        context.MonitoredSeries.AddRange(
            CreateSeries("Spice and Wolf"),
            CreateSeries("Fullmetal Alchemist"));
        await context.SaveChangesAsync();

        var results = await unitOfWork.MonitoredSeriesRepository.GetMonitoredSeriesDtosForUser(
            "", null, PaginationParams.Default, CancellationToken.None);

        Assert.Equal(2, results.TotalCount);
    }

    private static MonitoredSeries CreateSeries(string title, string? coverUrl = null,
        Provider provider = Provider.Nyaa)
    {
        return new MonitoredSeries
        {
            Id = Guid.NewGuid(),
            Title = title,
            Summary = string.Empty,
            BaseDir = "/base",
            CoverUrl = coverUrl,
            Provider = provider,
            ExternalId = string.Empty,
            HardcoverId = string.Empty,
            MangaBakaId = string.Empty,
            TitleOverride = string.Empty,
        };
    }
}
