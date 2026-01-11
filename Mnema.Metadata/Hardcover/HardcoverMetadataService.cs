using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
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

    public const string HardcoverBaseUrl = "https://hardcover.app";

    public Task<List<Series>> Search(MetadataSearchDto search, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Series?> GetSeries(string externalId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(externalId, out int seriesId))
        {
            throw new MnemaException($"{nameof(externalId)} is not an integer");
        }

        var request = new GraphQLRequest(GetSeriesById, new { id = seriesId });
        var response = await graphQlClient.SendQueryAsync<HardcoverGetSeriesInfoByIdResponse>(request, cancellationToken);

        var series = response.Data.Series;

        return new Series
        {
            Id = externalId,
            Title = series.Name,
            Summary = series.Description ?? string.Empty,
            Status = series.IsCompleted ?? false ? PublicationStatus.Completed : PublicationStatus.Unknown,
            Tags = [],
            People = series.People(),
            HighestVolumeNumber = series.IsCompleted ?? false ? series.BooksCount : null,
            RefUrl = $"{HardcoverBaseUrl}/series/{series.Slug}",
            Links = [$"{HardcoverBaseUrl}/series/{series.Slug}"],
            Chapters = series.BookSeries.Select(b =>
            {
                var book = b.Book;

                return new Chapter
                {
                    Id = book.Id.ToString(),
                    Title = book.Title,
                    Summary =  book.Description ?? string.Empty,
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
}
