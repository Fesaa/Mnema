using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Mnema.API;
using Mnema.API.Services;
using Mnema.Common.Exceptions;

namespace Mnema.Services;

internal sealed class DummyUser;

public class PasswordService(ISettingsService settingsService): IPasswordService
{
    private readonly PasswordHasher<DummyUser> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new DummyUser(), password);
    }

    public async Task<bool> VerifyHashedPassword(string password)
    {
        var passwordHash = await settingsService.GetSettingAsync(ServerSettingKeys.Password);
        if (string.IsNullOrEmpty(password))
            throw new BadRequestException();

        var result = _passwordHasher.VerifyHashedPassword(new DummyUser(), passwordHash, password);
        if (result is PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = _passwordHasher.HashPassword(new DummyUser(), password);
            await settingsService.SetSettingAsync(ServerSettingKeys.Password, newHash);
        }

        return result is PasswordVerificationResult.SuccessRehashNeeded or PasswordVerificationResult.Success;
    }
}
