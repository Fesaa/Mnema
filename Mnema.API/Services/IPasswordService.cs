using System.Threading.Tasks;

namespace Mnema.API.Services;

public interface IPasswordService
{
    string HashPassword(string password);
    Task<bool> VerifyHashedPassword(string password);
}
