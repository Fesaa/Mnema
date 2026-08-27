using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Helpers;
using Mnema.Metadata.Extensions;
using Mnema.Metadata.Hardcover.Generated;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.Publication;
using StrawberryShake;

namespace Mnema.Metadata.Hardcover;

public class HardcoverMetadataService(
    ILogger<HardcoverMetadataService> logger,
    IUnitOfWork unitOfWork,
    IHardcoverClient hardcoverClient
    ): IMetadataProviderService
{
    private const string HardcoverBaseUrl = "https://hardcover.app";

    public async Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        var page = paginationParams.PageNumber + 1;
        var perPage = paginationParams.PageSize;

        var searchResponse = await hardcoverClient.SearchSeries.ExecuteAsync(
            search.Query,
            page,
            perPage,
            cancellationToken);

        searchResponse.EnsureNoErrors();

        var searchData = searchResponse.Data?.Search;
        if (searchData?.Results == null)
        {
            return new PagedList<MetadataSearchResult>([], 0, paginationParams.PageNumber, perPage);
        }

        var seriesIds = searchData.Results?.GetProperty("hits").EnumerateArray()
            .Select(h => int.TryParse(h.GetProperty("document").GetProperty("id").GetString(), out var result) ? result : 0)
            .Where(i => i > 0)
            .ToList() ?? [];

        if (seriesIds.Count == 0)
        {
            return new PagedList<MetadataSearchResult>([], 0, (searchData.Page ?? 1) - 1, perPage);
        }

        var seriesResponse = await hardcoverClient.GetSeriesByIds.ExecuteAsync(seriesIds, cancellationToken);
        seriesResponse.EnsureNoErrors();

        var seriesList = seriesResponse.Data?.Series ?? [];

        var monitoredSeriesById = (await unitOfWork.MonitoredSeriesRepository
            .GetByHardcoverIds(seriesIds.Select(id => id.ToString()).ToList(), cancellationToken))
            .GroupBy(s => s.HardcoverId)
            .ToDictionary(s => s.Key, s => s.Select(m => m.Id).ToList());

        var settings = await unitOfWork.MetadataProviderSettingsRepository
            .GetMetadataProviderSettings(MetadataProvider.Hardcover, cancellationToken);

        var seriesResults = seriesList
            .Select(s => ConvertFromHardcoverSeries(settings, s, monitoredSeriesById));

        return new PagedList<MetadataSearchResult>(
            seriesResults,
            seriesList.Count,
            (searchData.Page ?? 1) - 1,
            perPage);
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(externalId, out var seriesId))
        {
            throw new MnemaException($"{nameof(externalId)} is not an integer");
        }

        var response = await hardcoverClient.GetSeriesById.ExecuteAsync(seriesId, cancellationToken);
        response.EnsureNoErrors();

        var series = response.Data?.Series;
        if (series == null) return null;

        var monitoredSeriesById = (await unitOfWork.MonitoredSeriesRepository
                .GetByHardcoverIds([series.Id.ToString()], cancellationToken))
            .GroupBy(s => s.HardcoverId)
            .ToDictionary(s => s.Key, s => s.Select(m => m.Id).ToList());

        var settings = await unitOfWork.MetadataProviderSettingsRepository.GetMetadataProviderSettings(MetadataProvider.Hardcover, cancellationToken);

        return ConvertFromHardcoverSeries(settings, series, monitoredSeriesById);
    }

    public Task<List<Cover>> GetCovers(string externalId, CancellationToken cancellationToken)
    {
        return Task.FromResult<List<Cover>>([]);
    }

    private static MetadataSearchResult ConvertFromHardcoverSeries(MetadataProviderSettings settings, ISeriesDetails series,
        Dictionary<string, List<Guid>> monitoredSeriesIds)
    {
        var realBooks = series.BookSeries.GroupBy(b => b.Position)
            .SelectMany<IGrouping<float?, IBookSeriesInfo>, IBookSeriesInfo>(g =>
            {
                if (g.Key == null)
                    return []; // Ignore books without a position

                var mostPopularBooks = g.MaxBy(b => b.Book!.UsersReadCount);

                return mostPopularBooks == null ? [] : [mostPopularBooks];
            })
            .ToList();

        var chapters = realBooks.Select(b =>
        {
            var book = b.Book;

            var edition = book!.Editions
                .OrderByDescending(e => e.Language?.Code == "en")
                .ThenByDescending(e => string.IsNullOrEmpty(e.Language?.Code))
                .FirstOrDefault();

            var contributions = (edition?.Contributions as IEnumerable<IContributionInfo>) ?? book.Contributions;

            return new Chapter
            {
                Id = book.Id.ToString(),
                Title = ParseChapterTitle(settings, book.Title ?? string.Empty, b.Position ?? 0),
                Summary = book.Description ?? string.Empty,
                CoverUrl = book.Image?.Url,
                RefUrl = $"{HardcoverBaseUrl}/id/book/{book.Id}",
                Isbn = edition?.Isbn,
                VolumeMarker = b.Position?.ToString() ?? string.Empty,
                ChapterMarker = string.Empty,
                SortOrder = b.Position,
                ReleaseDate = edition?.ReleaseDate?.ToUniversalTime() ?? b.Book?.ReleaseDate?.ToUniversalTime(),
                Tags = book.Taggings
                    .Select(t => t.Tag)
                    .Where(t => t.TagCategory.Category == "Genre")
                    .Select(t => new Tag
                    {
                        Id = t.Id.ToString(),
                        Value = t.Tag,
                        IsMarkedAsGenre = t.TagCategory.Category == "Genre",
                        MetadataProvider = MetadataProvider.Hardcover,
                    }).ToList(),
                People = contributions
                    .Where(c => c.Role != null)
                    .Select(c => new Person
                    {
                        Name = c.Author!.Name,
                        Roles = [c.Role!.Value]
                    }).ToList(),
                TranslationGroups = []
            };
        }).ToList();

        return new MetadataSearchResult
        {
            Id = series.Id.ToString(),
            MonitoredSeriesId = monitoredSeriesIds.GetValueOrDefault(series.Id.ToString()) ?? [],
            Title = CleanTitle(series.Name),
            Summary = series.Description ?? string.Empty,
            Status = series.IsCompleted ?? false ? PublicationStatus.Completed : PublicationStatus.Unknown,
            Tags = [],
            People = chapters.SelectMany(c => c.People).DistinctBy(p => p.Name).ToList(),
            HighestVolumeNumber = series.IsCompleted ?? false ? series.BooksCount : null,
            CoverUrl = series.BookSeries.FirstOrDefault(b => b.Book.Image != null)?.Book.Image?.Url,
            RefUrl = $"{HardcoverBaseUrl}/id/series/{series.Id}",
            Links = [$"{HardcoverBaseUrl}/id/series/{series.Id}"],
            Chapters = chapters,
        };
    }

    private static string CleanTitle(string title)
    {
        return title
            .Replace("(Manga)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("(Light Novel)", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    public static string ParseChapterTitle(MetadataProviderSettings settings, string title, float? position)
    {
        if (position == null) return string.Empty;

        var chapterTitle = CleanTitle(title);
        var subtitle = string.Empty;

        var volumePositionMarker = $"Vol. {position}";
        var volumePositionMarkerIndex =
            chapterTitle.IndexOf(volumePositionMarker, StringComparison.InvariantCultureIgnoreCase);

        if (volumePositionMarkerIndex > -1)
        {
            var subtitleStartIndex = volumePositionMarkerIndex + volumePositionMarker.Length;
            subtitle = chapterTitle[subtitleStartIndex..].Trim(':', ' ');
        }

        if (settings.GetKey(HardcoverMetadataConfiguration.OnlyUseSubtitleAsChapterTitle))
        {
            return subtitle;
        }

        return string.IsNullOrEmpty(subtitle) ? chapterTitle : subtitle;
    }
}
