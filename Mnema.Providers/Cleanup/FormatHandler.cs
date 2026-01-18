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

internal interface IFormatHandler
{
    Format SupportedFormat { get; }
    Task HandleAsync(FormatHandlerContext context);
}

internal record FormatHandlerContext(
    string SourceFile,
    string DestinationPath,
    string? CoverUrl,
    ComicInfo? ComicInfo,
    UserPreferences Preferences,
    DownloadRequestDto Request
);

internal class ArchiveFormatHandler(
    IFileSystem fileSystem,
    IImageService imageService,
    IParserService parserService,
    HttpClient httpClient
) : IFormatHandler
{
    private static readonly XmlSerializer ComicInfoSerializer = new(typeof(ComicInfo));
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff"];

    public Format SupportedFormat => Format.Archive;

    public async Task HandleAsync(FormatHandlerContext context)
    {
        using var tempDir = new TempDirectoryScope(fileSystem, context.DestinationPath);

        await ZipFile.ExtractToDirectoryAsync(context.SourceFile, tempDir.ExtractPath);

        var foundCover = await ProcessFilesAsync(context, tempDir);

        await AddMetadataAsync(context, tempDir.FinalPath);
        await AddCoverIfNeededAsync(context, tempDir.FinalPath, foundCover);

        await CreateFinalArchiveAsync(tempDir.FinalPath, context.DestinationPath);
    }

    private async Task<bool> ProcessFilesAsync(FormatHandlerContext context, TempDirectoryScope tempDir)
    {
        var sourceFiles = fileSystem.Directory.GetFiles(tempDir.ExtractPath, "*", SearchOption.AllDirectories);
        var foundCover = false;
        var coverLock = new Lock();

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
        await Parallel.ForEachAsync(sourceFiles, parallelOptions, async (file, _) =>
        {
            var fileName = fileSystem.Path.GetFileName(file);
            var ext = fileSystem.Path.GetExtension(file).ToLower();

            if (ImageExtensions.Contains(ext))
            {
                var localFoundCover = await ProcessImageFileAsync(context, file, fileName, foundCover, tempDir.FinalPath);

                if (localFoundCover)
                {
                    lock (coverLock)
                    {
                        foundCover = true;
                    }
                }
            }
            else if (!fileName.Equals("ComicInfo.xml", StringComparison.OrdinalIgnoreCase))
            {
                var destFilePath = fileSystem.Path.Join(tempDir.FinalPath, fileName);
                fileSystem.File.Copy(file, destFilePath, true);
            }
        });

        return foundCover;
    }

    private async Task<bool> ProcessImageFileAsync(
        FormatHandlerContext context,
        string file,
        string fileName,
        bool foundCover,
        string finalPath)
    {
        var destExt = context.Preferences.ImageFormat.GetFileExtension(fileName);
        var destImageName = fileSystem.Path.GetFileNameWithoutExtension(fileName) + destExt;
        var destImagePath = fileSystem.Path.Join(finalPath, destImageName);

        if (!foundCover && parserService.IsCoverImage(fileName))
        {
            destImagePath = fileSystem.Path.Join(finalPath, $"!0000 cover{destExt}");
            foundCover = true;
        }

        await using var stream = fileSystem.File.OpenRead(file);
        await imageService.ConvertAndSave(stream, context.Preferences.ImageFormat, destImagePath);

        return foundCover;
    }

    private async Task AddMetadataAsync(FormatHandlerContext context, string finalPath)
    {
        if (context.ComicInfo == null) return;

        var ciPath = fileSystem.Path.Join(finalPath, "ComicInfo.xml");
        await using var ciStream = fileSystem.File.OpenWrite(ciPath);
        await using var writer = new StreamWriter(ciStream);
        ComicInfoSerializer.Serialize(writer, context.ComicInfo);
    }

    private async Task AddCoverIfNeededAsync(FormatHandlerContext context, string finalPath, bool foundCover)
    {
        if (!context.Request.GetBool(RequestConstants.IncludeCover)) return;
        if (foundCover && !context.Request.GetBool(RequestConstants.UpdateCover)) return;
        if (string.IsNullOrEmpty(context.CoverUrl)) return;

        var filePath = fileSystem.Path.Join(finalPath, $"!0000 cover{fileSystem.Path.GetExtension(context.CoverUrl)}");
        await using var stream = await httpClient.GetStreamAsync(context.CoverUrl);
        await using var file = fileSystem.File.Create(filePath);
        await stream.CopyToAsync(file);
    }

    private async Task CreateFinalArchiveAsync(string finalPath, string destinationPath)
    {
        if (fileSystem.File.Exists(destinationPath))
            fileSystem.File.Delete(destinationPath);

        await ZipFile.CreateFromDirectoryAsync(finalPath, destinationPath, CompressionLevel.SmallestSize, false);
    }
}

internal class TempDirectoryScope : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly string _tempDirPath;

    public string ExtractPath { get; }
    public string FinalPath { get; }

    public TempDirectoryScope(IFileSystem fileSystem, string destinationPath)
    {
        _fileSystem = fileSystem;
        var tempDirName = fileSystem.Path.GetFileNameWithoutExtension(destinationPath);
        _tempDirPath = fileSystem.Path.Join(fileSystem.Path.GetTempPath(), "Mnema", tempDirName);
        ExtractPath = fileSystem.Path.Join(_tempDirPath, "extract");
        FinalPath = fileSystem.Path.Join(_tempDirPath, "final");

        InitializeDirectories();
    }

    private void InitializeDirectories()
    {
        if (_fileSystem.Directory.Exists(_tempDirPath))
            _fileSystem.Directory.Delete(_tempDirPath, true);

        _fileSystem.Directory.CreateDirectory(_tempDirPath);
        _fileSystem.Directory.CreateDirectory(ExtractPath);
        _fileSystem.Directory.CreateDirectory(FinalPath);
    }

    public void Dispose()
    {
        if (_fileSystem.Directory.Exists(_tempDirPath))
            _fileSystem.Directory.Delete(_tempDirPath, true);
    }
}
