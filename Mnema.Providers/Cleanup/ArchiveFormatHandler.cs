using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;
using Mnema.Models.External;

namespace Mnema.Providers.Cleanup;

internal class ArchiveFormatHandler(
    IFileSystem fileSystem,
    IImageService imageService,
    IParserService parserService,
    HttpClient httpClient
) : IFormatHandler
{
    private static readonly XmlSerializer ComicInfoSerializer = new(typeof(ComicInfo));

    public Format SupportedFormat => Format.Archive;

    public async Task HandleAsync(FormatHandlerContext context)
    {
        if (fileSystem.File.Exists(context.DestinationPath))
            fileSystem.File.Delete(context.DestinationPath);

        await using var destStream = fileSystem.File.Create(context.DestinationPath);
        await using var destArchive = new ZipArchive(destStream, ZipArchiveMode.Create, leaveOpen: false);

        await using var sourceStream = fileSystem.File.OpenRead(context.SourceFile);
        await using var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: false);

        var foundCover = await ProcessEntriesAsync(context, sourceArchive, destArchive);

        await AddMetadataAsync(context, destArchive);
        await AddCoverIfNeededAsync(context, destArchive, foundCover);
    }

    private async Task<bool> ProcessEntriesAsync(
        FormatHandlerContext context,
        ZipArchive sourceArchive,
        ZipArchive destArchive)
    {
        var foundCover = false;
        var coverLock = new object();

        foreach (var entry in sourceArchive.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue;

            var fileName = fileSystem.Path.GetFileName(entry.FullName);

            if (parserService.IsImage(fileName))
            {
                var localFoundCover = await ProcessImageEntryAsync(
                    context,
                    entry,
                    fileName,
                    foundCover,
                    destArchive);

                if (localFoundCover && !foundCover)
                {
                    lock (coverLock)
                    {
                        foundCover = true;
                    }
                }
            }
            else if (!fileName.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
            {
                await CopyEntryAsync(entry, fileName, destArchive);
            }
        }

        return foundCover;
    }

    private async Task<bool> ProcessImageEntryAsync(
        FormatHandlerContext context,
        ZipArchiveEntry sourceEntry,
        string fileName,
        bool foundCover,
        ZipArchive destArchive)
    {
        var isCover = !foundCover && parserService.IsCoverImage(fileName);
        var destExt = context.Preferences.ImageFormat.GetFileExtension(fileName);
        var destImageName = isCover
            ? $"!0000 cover{destExt}"
            : fileSystem.Path.GetFileNameWithoutExtension(fileName) + destExt;

        var destEntry = destArchive.CreateEntry(destImageName, CompressionLevel.SmallestSize);

        await using var sourceStream = await sourceEntry.OpenAsync();
        await using var destStream = await destEntry.OpenAsync();

        await imageService.Convert(sourceStream, context.Preferences.ImageFormat, destStream);

        return isCover;
    }

    private static async Task CopyEntryAsync(ZipArchiveEntry sourceEntry, string fileName, ZipArchive destArchive)
    {
        var destEntry = destArchive.CreateEntry(fileName, CompressionLevel.SmallestSize);

        await using var sourceStream = await sourceEntry.OpenAsync();
        await using var destStream = await destEntry.OpenAsync();
        await sourceStream.CopyToAsync(destStream);
    }

    private static async Task AddMetadataAsync(FormatHandlerContext context, ZipArchive destArchive)
    {
        if (context.ComicInfo == null) return;

        var entry = destArchive.CreateEntry("ComicInfo.xml", CompressionLevel.SmallestSize);
        await using var stream = await entry.OpenAsync();
        await using var writer = new StreamWriter(stream);
        ComicInfoSerializer.Serialize(writer, context.ComicInfo);
    }

    private async Task AddCoverIfNeededAsync(FormatHandlerContext context, ZipArchive destArchive, bool foundCover)
    {
        if (!context.Request.GetBool(RequestConstants.IncludeCover)) return;
        if (foundCover && !context.Request.GetBool(RequestConstants.UpdateCover)) return;
        if (string.IsNullOrEmpty(context.CoverUrl)) return;

        var ext = fileSystem.Path.GetExtension(context.CoverUrl);
        var entry = destArchive.CreateEntry($"!0000 cover{ext}", CompressionLevel.SmallestSize);

        await using var coverStream = await httpClient.GetStreamAsync(context.CoverUrl);
        await using var entryStream = await entry.OpenAsync();
        await coverStream.CopyToAsync(entryStream);
    }
}
