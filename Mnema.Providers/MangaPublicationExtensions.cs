namespace Mnema.Providers;

public class MangaPublicationExtensions: IPublicationExtensions
{
    public Task DownloadCallback(Publication publication)
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