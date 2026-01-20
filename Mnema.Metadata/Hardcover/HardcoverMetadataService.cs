using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Helpers;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.Metadata.Hardcover;

public class HardcoverMetadataService(
    ILogger<HardcoverMetadataService> logger,
    [FromKeyedServices(key: MetadataProvider.Hardcover)] IGraphQLClient graphQlClient
    ): IMetadataProviderService
{
    private const string HardcoverBaseUrl = "https://hardcover.app";

    public async Task<PagedList<Series>> Search(MetadataSearchDto search, PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var request = new GraphQLRequest(SearchSeries, new
        {
            query = search.Query,
            page = paginationParams.PageNumber + 1, // Hardcover is 1 based
            per_page = paginationParams.PageSize
        });

        var response = await graphQlClient.SendQueryAsync<HardcoverSearchSeriesResponse>(request, cancellationToken);
        if (response.Errors != null)
            throw new MnemaException($"{nameof(SearchSeries)} failed: {string.Join(",", response.Errors.Select(x => x.Message))}");

        // Hardcover returns basically nothing for series in its response... load them manually with a second query
        var seriesIds = response.Data.Series.Results.Hits
            .Select(h => int.TryParse(h.Item.Id, out var result) ? result : 0)
            .Where(i => i > 0);

        var seriesRequest = new GraphQLRequest(GetSeriesByIds, new { ids = seriesIds });
        var seriesResponse = await graphQlClient.SendQueryAsync<HardcoverGetSeriesInfoByIdsResponse>(seriesRequest, cancellationToken);
        if (seriesResponse.Errors != null)
            throw new MnemaException($"{nameof(SearchSeries)} failed: {string.Join(",", seriesResponse.Errors.Select(x => x.Message))}");

        var series = seriesResponse.Data.Series.Select(ConvertFromHardcoverSeries);

        return new PagedList<Series>(series, response.Data.Series.Results.Found, response.Data.Series.Page - 1, response.Data.Series.PageSize);
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(externalId, out int seriesId))
        {
            throw new MnemaException($"{nameof(externalId)} is not an integer");
        }

        var request = new GraphQLRequest(GetSeriesById, new { id = seriesId });
        var response = await graphQlClient.SendQueryAsync<HardcoverGetSeriesInfoByIdResponse>(request, cancellationToken);
        if (response.Errors != null)
            throw new MnemaException($"{nameof(GetSeries)} failed: {string.Join(",", response.Errors.Select(x => x.Message))}");

        var series = response.Data.Series;

        return ConvertFromHardcoverSeries(series);
    }

    private static Series ConvertFromHardcoverSeries(HardcoverSeries series)
    {
        var realBooks = series.BookSeries.GroupBy(b => b.Position)
            .SelectMany(g =>
            {
                if (g.Key == null)
                    return g;

                var featuredBook = g.FirstOrDefault(b => b.Featured);
                if (featuredBook != null)
                    return [featuredBook];

                var fallBack = g.MaxBy(b => b.Book.UserReadCount);

                return fallBack == null ? [] : [fallBack];
            })
            .ToList();

        return new Series
        {
            Id = series.Id.ToString(),
            Title = series.Name,
            Summary = series.Description ?? string.Empty,
            Status = series.IsCompleted ?? false ? PublicationStatus.Completed : PublicationStatus.Unknown,
            Tags = [],
            People = series.People(),
            HighestVolumeNumber = series.IsCompleted ?? false ? series.BooksCount : null,
            CoverUrl = series.BookSeries.FirstOrDefault(b => b.Book.Image != null)?.Book.Image?.Url,
            RefUrl = $"{HardcoverBaseUrl}/series/{series.Slug}",
            Links = [$"{HardcoverBaseUrl}/series/{series.Slug}"],
            Chapters = realBooks.Select(b =>
            {
                var book = b.Book;

                return new Chapter
                {
                    Id = book.Id.ToString(),
                    Title = book.Title,
                    Summary = book.Description ?? string.Empty,
                    CoverUrl = book.Image?.Url,
                    RefUrl = $"{HardcoverBaseUrl}/books/{book.Slug}",
                    VolumeMarker = b.Position?.ToString() ?? string.Empty,
                    ChapterMarker = string.Empty,
                    Tags = book.Taggings
                        .Select(t => t.Tag)
                        .Where(t => t.TagCategory.Category == HardcoverTagCategory.Genre)
                        .Select(t => new Tag
                        {
                            Id = t.Id.ToString(),
                            Value = t.Tag,
                            IsMarkedAsGenre = t.TagCategory.Category == HardcoverTagCategory.Genre,
                            MetadataProvider = MetadataProvider.Hardcover,
                        }).ToList(),
                    People = book.Contributions
                        .Where(c => c.Role != null)
                        .Select(c => new Person
                        {
                            Name = c.Author.Name,
                            Roles = [c.Role!.Value]
                        }).ToList(),
                    TranslationGroups = []
                };
            }).ToList(),
        };
    }

    private static readonly GraphQlQueryLoader QueryLoader =
        GraphQlHelper.CreateLoaderForNamespace(typeof(HardcoverMetadataService).Assembly,
            "Mnema.Metadata.Hardcover.Queries");

    private static readonly GraphQLQuery GetSeriesById = QueryLoader(nameof(GetSeriesById));
    private static readonly GraphQLQuery SearchSeries = QueryLoader(nameof(SearchSeries));
    private static readonly GraphQLQuery GetSeriesByIds = QueryLoader(nameof(GetSeriesByIds));
}
