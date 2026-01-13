using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.User;

namespace Mnema.API;

public interface IImageService
{

    Task ConvertAndSave(Stream stream, ImageFormat format, string filePath,
        CancellationToken cancellationToken = default);

}
