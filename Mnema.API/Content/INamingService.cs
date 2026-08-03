using System;
using System.Collections.Generic;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface INamingService
{
    [Obsolete("Do not use for new stuff, only for MP compat")]
    string GetVolumeDirectoryName(string title, string volumeMarker);

    string GetChapterFilePath(string baseDir, string title, string fileName);

    string GetChapterFileName(string title, Chapter chapter);

    string GetChapterFileName(
        string title,
        string? volumeMarker,
        string chapterMarker,
        float? chapterNumber,
        bool isOneShot,
        string? chapterTitle,
        IReadOnlyCollection<string> existingPaths);
}
