using System.IO.Compression;
using System.IO.Pipelines;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Common.Helpers;
using Mnema.Models.External;

namespace Mnema.Providers;

internal interface IPublicationExtensions
{
    OnDiskContent? ParseOnDiskFile(string fileName);
    
    string? ParseVolumeFromFile(Publication publication, OnDiskContent content);
    
    Task<string> DownloadCallback(IoWork ioWork, CancellationToken cancellationToken);
    
    Task Cleanup(string path);

}

internal interface IPreDownloadHook
{
    Task PreDownloadHook(Publication publication, IServiceScope scope, CancellationToken cancellationToken);
}

internal partial class MangaPublicationExtensions: IPublicationExtensions
{
    private static readonly Regex ContentVolumeAndChapterRegex = MyContentVolumeAndChapterRegex();
    
    private static readonly Regex ContentChapterRegex = MyContentChapterRegex();
    
    private static readonly Regex ContentVolumeRegex = MyContentVolumeRegex();
    
    public async Task<string> DownloadCallback(IoWork ioWork, CancellationToken cancellationToken)
    {
        var fileType = Path.GetExtension(ioWork.Url);

        var fileCounter = $"{ioWork.Idx}".PadLeft(4, '0');
        var filePath = Path.Join(ioWork.FilePath, $"page {fileCounter}{fileType}");

        await using (ioWork.Stream)
        {
            await using var file = File.Create(filePath);
            await ioWork.Stream.CopyToAsync(file, cancellationToken);
        }

        return filePath;
    }

    public OnDiskContent? ParseOnDiskFile(string fileName)
    {
        // Try volume and chapter
        var match = ContentVolumeAndChapterRegex.Match(fileName);
        if (match is {Success: true, Groups.Count: 3})
        {
            return new OnDiskContent
            {
                Volume = TrimLeadingZero(match.Groups[1].Value),
                Chapter = TrimLeadingZero(match.Groups[2].Value)
            };
        }

        // Try volume only
        match = ContentVolumeRegex.Match(fileName);
        if (match is {Success: true, Groups.Count: 2})
        {
            return new OnDiskContent
            {
                Volume = TrimLeadingZero(match.Groups[1].Value)
            };
        }

        // Try chapter only
        match = ContentChapterRegex.Match(fileName);
        if (match is {Success: true, Groups.Count: 2})
        {
            return new OnDiskContent
            {
                Chapter = TrimLeadingZero(match.Groups[1].Value)
            };
        }

        // Fallback to simple ext check
        if (Path.GetExtension(fileName).Equals(".cbz", StringComparison.OrdinalIgnoreCase))
        {
            return new OnDiskContent();
        }

        return null;
    }
    
    private static string TrimLeadingZero(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.Trim().TrimStart('0');

    }

    public async Task Cleanup(string path)
    {
        await ZipFile.CreateFromDirectoryAsync(path, path + ".cbz",
            CompressionLevel.SmallestSize, includeBaseDirectory: false);
        
        Directory.Delete(path, true);
    }

    public string? ParseVolumeFromFile(Publication publication, OnDiskContent content)
    {
        var zipFile = ZipFile.OpenRead(content.Path);

        var archiveEntry = zipFile.Entries
            .FirstOrDefault(e => e.Name.Equals("ComicInfo.xml", StringComparison.InvariantCultureIgnoreCase));

        if (archiveEntry == null) return null;

        var comicInfo = XmlHelper.Deserialize<ComicInfo>(archiveEntry.Open());

        return comicInfo?.Volume;
    }

    [GeneratedRegex(@".* (?:Vol\. ([\d\.]+)) (?:Ch)\. ([\d\.]+)\.cbz", RegexOptions.Compiled)]
    private static partial Regex MyContentVolumeAndChapterRegex();
    [GeneratedRegex(@".* Ch\. ([\d\.]+)\.cbz", RegexOptions.Compiled)]
    private static partial Regex MyContentChapterRegex();
    [GeneratedRegex(@".* Vol\. ([\d\.]+)\.cbz", RegexOptions.Compiled)]
    private static partial Regex MyContentVolumeRegex();
}