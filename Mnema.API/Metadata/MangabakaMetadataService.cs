using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common.StringFormatter;
using Mnema.Models.DTOs;

namespace Mnema.API.Metadata;

public interface IMangabakaMetadataService : IMetadataProviderService
{
    IStringFormatter<string> NativeLanguageFormatter { get; }

    Task<int?> FindMangabakaSeries(int? aniListId, string? malId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all collections of a Mangabaka series, so a specific one can be linked to a monitored series
    /// </summary>
    /// <param name="externalId">The Mangabaka series id</param>
    /// <param name="cancellationToken"></param>
    Task<List<MangabakaCollectionDto>> GetCollections(string externalId, CancellationToken cancellationToken);
}
