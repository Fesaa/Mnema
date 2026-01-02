
using Mnema.API.Content;

namespace Mnema.API;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectoryAsync(Func<string, OnDiskContent?> diskParser, string path, CancellationToken cancellationToken);
}