using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Mnema.API;
using Mnema.API.Services;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities;

namespace Mnema.Services;

internal sealed class DummyUser;

public class PasswordService(IUnitOfWork unitOfWork): IPasswordService
{
    private readonly PasswordHasher<DummyUser> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new DummyUser(), password);
    }

    public async Task<bool> VerifyHashedPassword(string password)
    {
        var passwordHash = await unitOfWork.SettingsRepository.GetSettingsAsync(ServerSettingKey.Password);
        if (string.IsNullOrEmpty(password))
            throw new BadRequestException();

        var result = _passwordHasher.VerifyHashedPassword(new DummyUser(), passwordHash.Value, password);
        if (result is PasswordVerificationResult.SuccessRehashNeeded)
        {
            var newHash = _passwordHasher.HashPassword(new DummyUser(), password);

            passwordHash.Value = newHash;
            unitOfWork.SettingsRepository.Update(passwordHash);

            await unitOfWork.CommitAsync();
        }

        return result is PasswordVerificationResult.SuccessRehashNeeded or PasswordVerificationResult.Success;
    }
}
