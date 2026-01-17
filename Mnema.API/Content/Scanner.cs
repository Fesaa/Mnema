using System.Collections.Generic;
using System.Threading;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectoryAsync(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    OnDiskContent ParseContent(string file, ContentFormat contentFormat);
}
