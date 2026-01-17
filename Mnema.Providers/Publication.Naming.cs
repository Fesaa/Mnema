using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication
{
    private readonly INamingService _namingService = scope.ServiceProvider.GetRequiredService<INamingService>();

    private string VolumeDir(Chapter chapter)
        => _namingService.GetVolumeDirectoryName(Title, chapter.VolumeMarker);

    private string ChapterPath(Chapter chapter)
        => _namingService.GetChapterFilePath(
            Request.BaseDir,
            Title,
            ChapterFileName(chapter));

    private string ChapterFileName(Chapter chapter)
        => _namingService.GetChapterFileName(
            Title,
            chapter.VolumeMarker,
            chapter.ChapterMarker,
            chapter.ChapterNumber(),
            chapter.IsOneShot,
            chapter.Title,
            DownloadedPaths.AsReadOnly());
}
