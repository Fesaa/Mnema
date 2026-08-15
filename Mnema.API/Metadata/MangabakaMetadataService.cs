using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common.StringFormatter;

namespace Mnema.API.Metadata;

public interface IMangabakaMetadataService : IMetadataProviderService
{
    IStringFormatter<string> NativeLanguageFormatter { get; }

    Task<int?> FindMangabakaSeries(int? aniListId, string? malId, CancellationToken cancellationToken);
}
