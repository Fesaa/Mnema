using System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace Mnema.Providers;

internal interface IPublicationExtensions
{
    OnDiskContent? ParseOnDiskFile(string fileName);
    
    string? ParseVolumeFromFile(Publication publication, OnDiskContent content);
    
    Task<string> DownloadCallback(Publication publication, IoWork ioWork, CancellationToken cancellationToken);
    
    Task Cleanup(Publication publication, string path);

}

internal interface IPreDownloadHook
{
    Task PreDownloadHook(Publication publication);
}

internal class MangaPublicationExtensions: IPublicationExtensions
{
    public async Task<string> DownloadCallback(Publication publication, IoWork ioWork, CancellationToken cancellationToken)
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
        throw new NotImplementedException();
    }
    public Task Cleanup(Publication publication, string path)
    {
        throw new NotImplementedException();
    }
    public string ParseVolumeFromFile(Publication publication, OnDiskContent content)
    {
        throw new NotImplementedException();
    }
}