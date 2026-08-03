using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers.Managers.Publication;

internal partial class Publication
{
    private readonly INamingService _namingService = scope.ServiceProvider.GetRequiredService<INamingService>();

    [Obsolete("Do not use for new stuff, only for MP compat")]
    private string VolumeDir(Chapter chapter)
        => _namingService.GetVolumeDirectoryName(Title, chapter.VolumeMarker);

    private string ChapterPath(Chapter chapter)
        => _namingService.GetChapterFilePath(Request.BaseDir, Title, ChapterFileName(chapter));

    private string ChapterFileName(Chapter chapter)
        => _namingService.GetChapterFileName(Preferences, Title, chapter, DownloadedPaths.AsReadOnly());
}
