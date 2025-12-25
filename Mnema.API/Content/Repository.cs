using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public sealed record DownloadUrl(string Url, string FallbackUrl);

public interface IRepository
{

    /// <summary>
    /// Search for possible series to download given a request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="pagination"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Mnema.Common.Exceptions.MnemaException">If something outside our control fails</exception>
    Task<PagedList<SearchResult>> SearchPublications(SearchRequest request, PaginationParams pagination, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve series information by id
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Mnema.Common.Exceptions.MnemaException">If something outside our control fails</exception>
    Task<Series> SeriesInfo(DownloadRequestDto request, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieve all urls that should be downloaded for a series 
    /// </summary>
    /// <param name="chapter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Mnema.Common.Exceptions.MnemaException">If something outside our control fails</exception>
    Task<IList<DownloadUrl>> ChapterUrls(Chapter chapter, CancellationToken cancellationToken);

    /// <summary>
    /// Get <see cref="DownloadMetadata"/> for the provider
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<DownloadMetadata> DownloadMetadata(CancellationToken cancellationToken);
    /// <summary>
    /// Get all <see cref="ModifierDto"/>s avaible for search for the provider
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<ModifierDto>> Modifiers(CancellationToken cancellationToken);

}