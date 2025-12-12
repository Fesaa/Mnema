namespace Mnema.Providers;

public class MangaPublicationExtensions: IPublicationExtensions
{
    public Task DownloadCallback()
    {
        throw new NotImplementedException();
    }
    public OnDiskContent? ParseOnDiskFile(string fileName)
    {
        throw new NotImplementedException();
    }
    public Task Cleanup(string path)
    {
        throw new NotImplementedException();
    }
}