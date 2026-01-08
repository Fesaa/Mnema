using System.IO;
using System.Threading.Tasks;
using Mnema.Models.Entities.User;

namespace Mnema.API;

public interface IImageService
{

    Stream ConvertFromStream(Stream stream, ImageFormat format);

}
