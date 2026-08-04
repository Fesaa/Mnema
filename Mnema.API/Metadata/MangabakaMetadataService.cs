using Mnema.API.Content;
using Mnema.Common.StringFormatter;

namespace Mnema.API.Metadata;

public interface IMangabakaMetadataService : IMetadataProviderService
{
    IStringFormatter<string> NativeLanguageFormatter { get; }
}
