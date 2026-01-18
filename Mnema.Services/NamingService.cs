using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Extensions;
using Mnema.Models.Internal;

namespace Mnema.Services;

public class NamingService(ILogger<NamingService> logger, ApplicationConfiguration configuration) : INamingService
{
    public string GetVolumeDirectoryName(string title, string volumeMarker)
        => $"{title} Vol. {volumeMarker}";

    public string GetChapterFilePath(string baseDir, string title, string fileName)
        => Path.Join(configuration.DownloadDir, baseDir, title, fileName);

    public string GetChapterFileName(
        string title,
        string? volumeMarker,
        string chapterMarker,
        float? chapterNumber,
        bool isOneShot,
        string? chapterTitle,
        IReadOnlyCollection<string> existingPaths)
    {
        return isOneShot
            ? GetOneShotFileName(title, chapterTitle, existingPaths)
            : GetDefaultFileName(title, volumeMarker, chapterMarker, chapterNumber);
    }

    private string GetDefaultFileName(string title, string? volumeMarker, string chapterMarker, float? chapterNumber)
    {
        var fileName = title;

        if (!string.IsNullOrEmpty(volumeMarker))
            fileName += $" Vol. {volumeMarker}";

        if (string.IsNullOrEmpty(chapterMarker))
            return fileName;

        if (chapterNumber == null)
        {
            logger.LogWarning("Failed to parse chapter number for marker {ChapterMarker}, not padding", chapterMarker);
            return $"{fileName} Ch. {chapterMarker}";
        }

        return $"{fileName} Ch. {chapterMarker.PadFloat(4)}";
    }

    private string GetOneShotFileName(string title, string? chapterTitle, IReadOnlyCollection<string> existingPaths)
    {
        var fileName = $"{title} {chapterTitle}".Trim();

        var idx = 0;
        var finalFileName = fileName;
        while (existingPaths.Contains(finalFileName))
        {
            finalFileName = $"{fileName} ({idx})";

            if (idx >= 25)
            {
                logger.LogWarning("More than 25 one shots with the same name for {Title}, generating random number", title);
                finalFileName = $"{fileName} ({Random.Shared.Next()})";
                break;
            }

            idx++;
        }

        return finalFileName;
    }
}
