using System.Threading.Tasks;

namespace Mnema.API.Content;

public interface ICleanupService
{
    Task Cleanup(IContent content);
}
