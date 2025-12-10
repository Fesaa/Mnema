using Mnema.Models.Entities.User;

namespace Mnema.API;

[Flags]
public enum UserIncludes
{
    None = 1 << 0,
    Subscriptions = 2 << 0,
    Pages = 3 << 0,
    Preferences = 4 << 0,
}

public interface IUserRepository
{
    Task<MnemaUser> GetUserById(Guid id, UserIncludes includes = UserIncludes.Preferences);
}