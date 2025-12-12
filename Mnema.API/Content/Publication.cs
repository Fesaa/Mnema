namespace Mnema.API.Content;

public interface IPublicationManager: IContentManager
{
    Task<IPublication> GetPublicationById(string id);

}

public interface IPublication: IContent
{

    Task LoadMetadataAsync(CancellationToken cancellationToken);
    Task DownloadContentAsync(CancellationToken cancellation);

}