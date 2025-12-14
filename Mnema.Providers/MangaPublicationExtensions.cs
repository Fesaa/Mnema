namespace Mnema.Providers;

internal interface IPublicationExtensions
{
    OnDiskContent? ParseOnDiskFile(string fileName);
    
    string? ParseVolumeFromFile(Publication publication, OnDiskContent content);
    
    Task DownloadCallback(Publication publication, IoWork ioWork);
    
    Task Cleanup(Publication publication, string path);

}

internal interface IPreDownloadHook
{
    Task PreDownloadHook(Publication publication);
}

internal class MangaPublicationExtensions: IPublicationExtensions
{
    public Task DownloadCallback(Publication publication, IoWork ioWork)
    {
        throw new NotImplementedException();
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