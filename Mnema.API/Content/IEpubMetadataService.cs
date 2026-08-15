using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.External;

namespace Mnema.API.Content;

public interface IEpubMetadataService
{
    ComicInfo? ReadComicInfo(string filePath, CancellationToken cancellationToken);
    Task WriteComicInfo(ComicInfo comicInfo, string filePath, CancellationToken cancellationToken);
}
