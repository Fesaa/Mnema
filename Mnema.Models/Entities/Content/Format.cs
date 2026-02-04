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

public static class ContentFormatExtensions
{
    public static string FileExt(this ContentFormat contentFormat)
    {
        return contentFormat switch
        {
            ContentFormat.Manga => ".cbz",
            ContentFormat.LightNovel => ".epub",
            ContentFormat.Book => ".epub",
            ContentFormat.Comic => ".cbz",
            _ => throw new ArgumentOutOfRangeException(nameof(contentFormat), contentFormat, null)
        };
    }
}
