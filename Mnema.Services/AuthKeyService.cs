using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Authentication;

namespace Mnema.Services;

public partial class AuthKeyService(IUnitOfWork unitOfWork): IAuthKeyService
{

    private static readonly Regex AuthKeyRegex = MyRegex();

    public async Task CreateAuthKey(AuthKeyDto dto, CancellationToken cancellationToken)
    {
        if (!AuthKeyRegex.IsMatch(dto.Key))
            throw new BadRequestException("Invalid auth key");

        var authkey = new AuthKey
        {
            Name = dto.Name,
            Roles = dto.Roles.ToList(),
            Key = dto.Key
        };

        unitOfWork.AuthKeyRepository.Add(authkey);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task UpdateAuthKey(AuthKeyDto dto, CancellationToken cancellationToken)
    {
        var authKey = await unitOfWork.AuthKeyRepository.GetById(dto.Id, cancellationToken);
        if (authKey == null) throw new NotFoundException();

        if (!MyRegex().IsMatch(dto.Key))
            throw new BadRequestException("Invalid auth key");

        authKey.Key = dto.Key;
        authKey.Name = dto.Name;
        authKey.Roles = dto.Roles.ToList();

        unitOfWork.AuthKeyRepository.Update(authKey);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public List<FormFieldDefinition> GetAuthKeyForm(ClaimsPrincipal principal)
    {
        return
        [
            new TextFieldDefinition
            {
                Key = "name",
                Field = "name",
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(4)
                    .WithMaxLength(32)
                    .Build(),
            },
            new TextFieldDefinition
            {
                Key = "key",
                Field = "key",
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(8)
                    .WithMaxLength(256)
                    .WithPattern(@"^[a-zA-Z0-9!\$%()*+,\-./:;<=>@\[\\\]^_`{|}~]+$")
                    .Build(),
            },
            new MultiTextFieldDefinition
            {
                Key = "roles",
                Field = "roles",
                ForceSingle = true,
                Options = principal.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Select(r => SelectOption<string>.Option(r, r))
                    .ToList(),
            },
        ];
    }

    [GeneratedRegex(@"^[a-zA-Z0-9!\$%()*+,\-./:;<=>@\[\\\]^_`{|}~]+$")]
    private static partial Regex MyRegex();
}
