using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication
{

    private string VolumeDir(Chapter chapter) => $"{Title} Vol. {chapter.VolumeMarker}";

    private string ChapterPath(Chapter chapter)
    {
        var basePath = Path.Join(_configuration.DownloadDir, Request.BaseDir, Title);

        if (!string.IsNullOrEmpty(chapter.VolumeMarker) && !true) // TODO: Port config switches
        {
            basePath = Path.Join(basePath, VolumeDir(chapter));
        }

        return Path.Join(basePath, ChapterFileName(chapter));
    }

    private string ChapterFileName(Chapter chapter) => chapter.IsOneShot ? OneShotFileName(chapter) : DefaultFileName(chapter);

    private string DefaultFileName(Chapter chapter)
    {
        var fileName = Title;

        if (!string.IsNullOrEmpty(chapter.VolumeMarker) && ShouldIncludeVolumeMarker())
        {
            fileName += $" Vol. {chapter.VolumeMarker}";
        }

        if (chapter.ChapterNumber() == null)
        {
            _logger.LogWarning("Failed to parse chapter for {ChapterId} not padding", chapter.Id);
            return $"{fileName} Ch. {chapter.ChapterMarker}";
        }

        return $"{fileName} Ch. {chapter.ChapterMarker.PadFloat(4)}";
    }

    private bool ShouldIncludeVolumeMarker()
    {
        // TODO: Port config switches
        if (true) return true;

        if (_hasDuplicateVolumes != null)
        {
            return _hasDuplicateVolumes.Value;
        }

        _hasDuplicateVolumes = Series.Chapters
            .GroupBy(c => c.ChapterMarker)
            .Select(g => g.Count())
            .Any(amount => amount > 1);

        return _hasDuplicateVolumes.Value;
    }

    private string OneShotFileName(Chapter chapter)
    {
        var fileName = $"{Title} {chapter.Title}".Trim();
        
        if (!false)  // TODO: Port config switches
        {
            fileName += " (OneShot)";
        }

        var idx = 0;
        var finalFileName = fileName;
        while (DownloadedPaths.Contains(finalFileName))
        {
            finalFileName = $"{fileName} ({idx})";

            if (idx >= 25)
            {
                _logger.LogWarning("More than 25 oneshots with the same name, generating random number");
                finalFileName = $"{fileName} ({Random.Shared.Next()})";
                break;
            }

            idx++;
        }

        return finalFileName;
    }

}