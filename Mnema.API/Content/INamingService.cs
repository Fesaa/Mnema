using System;
using System.Collections.Generic;
using Mnema.Common.StringFormatter;
using Mnema.Models.Entities;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public record ChapterNameContext(string Title, Chapter Chapter);

public interface INamingService
{
    public const string DefaultChapterFormat = "{Title}[ Vol. {Volume}][ Ch. {Chapter:#4}]";
    public const string DefaultOneShotFormat = "{Title}[ {ChapterTitle}]";

    IStringFormatter<ChapterNameContext> ChapterFormatter { get; }
    IStringFormatter<ChapterNameContext> OneShotFormatter { get; }

    [Obsolete("Do not use for new stuff, only for MP compat")]
    string GetVolumeDirectoryName(string title, string volumeMarker);

    string GetChapterFilePath(string baseDir, string title, string fileName);

    string GetChapterFileName(Preferences preferences, string title, Chapter chapter, IReadOnlyCollection<string>? existingPaths = null);
}
