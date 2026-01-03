
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mnema.API.Content;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectoryAsync(Func<string, OnDiskContent?> diskParser, string path, CancellationToken cancellationToken);
}