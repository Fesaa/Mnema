using Mnema.Models.Publication;

namespace Mnema.Providers.Publication;

public sealed record DownloadUrl(string Url, string FallbackUrl);

public interface IRepository
{
    /// <summary>
    /// Retrieve series information by id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Series SeriesInfo(string id, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieve all urls that should be downloaded for a series 
    /// </summary>
    /// <param name="chapter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IList<DownloadUrl> ChapterUrls(Chapter chapter, CancellationToken cancellationToken);

}