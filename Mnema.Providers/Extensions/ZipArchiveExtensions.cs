using System;
using System.IO.Compression;
using System.Linq;

namespace Mnema.Providers.Extensions;

public static class ZipArchiveExtensions
{

    extension(ZipArchive archive)
    {
        public ZipArchiveEntry? GetComicInfo()
        {
            return archive.GetEntry("ComicInfo.xml") ??
                   archive.Entries
                       .FirstOrDefault(e => e.Name.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase));
        }
    }

}
