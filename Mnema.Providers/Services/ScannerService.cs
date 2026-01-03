using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Internal;

namespace Mnema.Providers.Services;

public class ScannerService(ILogger<ScannerService> logger, IFileSystem fileSystem, ApplicationConfiguration configuration): IScannerService
{
    public List<OnDiskContent> ScanDirectoryAsync(Func<string, OnDiskContent?> diskParser, string path, CancellationToken cancellationToken)
    {
        var fullPath = Path.Join(configuration.BaseDir, path);
        if (!fileSystem.Directory.Exists(fullPath)) return [];

        var contents = new List<OnDiskContent>();
        
        foreach (var entry in fileSystem.Directory.EnumerateFileSystemEntries(fullPath))
        {
            if (cancellationToken.IsCancellationRequested) return [];

            if (fileSystem.Directory.Exists(entry))
            {
                contents.AddRange(ScanDirectoryAsync(diskParser, entry, cancellationToken));
                continue;
            }

            var content = diskParser(entry);
            if (content == null)
            {
                logger.LogTrace("Ignoring {FileName} on disk", entry);
                continue;
            }
            
            logger.LogTrace("Adding {FileName} to on disk content. (Vol. {Volume} Ch. {Chapter})", entry, content.Volume, content.Chapter);
            
            contents.Add(new OnDiskContent
            {
                Name = Path.GetFileNameWithoutExtension(entry),
                Path = entry,
                Volume = content.Volume,
                Chapter = content.Chapter,
            });
        }

        return contents;
    }
}