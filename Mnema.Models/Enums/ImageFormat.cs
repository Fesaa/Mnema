using System;
using Mnema.Common.Extensions;

namespace Mnema.Models.Entities.User;

public enum ImageFormat
{
    Upstream = 0,
    Webp = 1
}

public static class ImageFormatExtensions
{
    public static string GetFileExtension(this ImageFormat imageFormat, string fileName)
    {
        return imageFormat switch
        {
            ImageFormat.Upstream => fileName.GetFileType(),
            ImageFormat.Webp => ".webp",
            _ => throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null)
        };
    }
}
