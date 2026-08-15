using Mnema.Models.Enums;

namespace Mnema.API.Content;

public interface IGroupedReleaseDetector
{
    bool IsGroupedRelease(Provider provider, string releaseName);
}
