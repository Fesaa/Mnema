using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.External;
using Mnema.Models.Entities;
using Mnema.Models.Enums;
using Mnema.Models.External;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface IMetadataService
{
    /// <summary>
    /// Construct the comicinfo with the given data
    /// </summary>
    /// <param name="preferences"></param>
    /// <param name="request"></param>
    /// <param name="title"></param>
    /// <param name="series"></param>
    /// <param name="chapter"></param>
    /// <param name="note"></param>
    /// <returns></returns>
    ComicInfo? CreateComicInfo(Preferences preferences, DownloadRequestDto request, string title, Series? series, Chapter? chapter, string? note = null);

    ComicInfo? ParseComicInfoFromFile(string file);

    Task WriteComicInfo(ComicInfo comicInfo, string filePath, CancellationToken cancellationToken);
}

public interface IMetadataProviderService
{
    /// <summary>
    /// Given the search parameters, return the results for the external provider
    /// </summary>
    /// <param name="search"></param>
    /// <param name="paginationParams"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>This may be cached</remarks>
    Task<PagedList<MetadataSearchResult>> Search(MetadataSearchDto search, PaginationParams paginationParams, CancellationToken cancellationToken);

    /// <summary>
    /// Return all known metadata for a given entity from the external provider
    /// </summary>
    /// <param name="externalId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>It is assumed this is cached</remarks>
    Task<Series?> GetSeries(string externalId, CancellationToken cancellationToken);

    /// <summary>
    /// Retruns all covers for a given entity from the external provider
    /// </summary>
    /// <param name="externalId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<Cover>> GetCovers(string externalId, CancellationToken cancellationToken);
}

public interface IMetadataResolver
{
    Task<Series?> ResolveSeriesAsync(Provider providers, MetadataBag metadata, CancellationToken cancellationToken = default);
    ChapterResolutionResult ResolveChapter(string fileName, Series? series, ContentFormat contentFormat);
}

public static class MetadataResolverOptions
{
    public static readonly IMetadataKey<bool> MergeIntoUpstream = MetadataKeys.Bool("merge-into-upstream");
    public static readonly IMetadataKey<bool> EnrichWithCovers = MetadataKeys.Bool("enrich-with-covers");
}

public record ChapterResolutionResult(string? Volume, string? Chapter, Chapter? ChapterEntity);
