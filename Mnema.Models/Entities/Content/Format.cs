using System;

namespace Mnema.Models.Entities.Content;

public enum Format
{
    Archive = 0,
    Epub = 1,

    Unsupported = 999999,
}

public enum ContentFormat
{
    Manga = 0,
    LightNovel = 1,
    Book = 2,
    Comic = 3,
}

public static class FormatExtensions
{
    public static string FileExt(this Format contentFormat)
    {
        return contentFormat switch
        {
            Format.Archive => ".cbz",
            Format.Epub => ".epub",
            _ => throw new ArgumentOutOfRangeException(nameof(contentFormat), contentFormat, null)
        };
    }
}
