using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IGroupedReleaseDetector
{
    bool IsGroupedRelease(Provider provider, string releaseName);
}
