using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;

namespace Mnema.Services;

public partial class AuthKeyService(IUnitOfWork unitOfWork): IAuthKeyService
{

    private static readonly Regex AuthKeyRegex = MyRegex();

    public async Task CreateAuthKey(Guid userId, AuthKeyDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var roles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        if (!AuthKeyRegex.IsMatch(dto.Key))
            throw new BadRequestException("Invalid auth key");

        var authkey = new AuthKey
        {
            UserId = userId,
            Name = dto.Name,
            Roles = dto.Roles.Where(roles.Contains).ToList(),
            Key = dto.Key
        };

        unitOfWork.AuthKeyRepository.Add(authkey);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task UpdateAuthKey(Guid id, AuthKeyDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var roles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var authKey = await unitOfWork.AuthKeyRepository.GetById(id, cancellationToken);
        if (authKey == null) throw new NotFoundException();

        if (authKey.UserId != dto.UserId) throw new UnauthorizedAccessException();

        if (!MyRegex().IsMatch(dto.Key))
            throw new BadRequestException("Invalid auth key");

        authKey.Key = dto.Key;
        authKey.Name = dto.Name;
        authKey.Roles = dto.Roles.Where(roles.Contains).ToList();

        unitOfWork.AuthKeyRepository.Update(authKey);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public List<FormControlDefinition> GetAuthKeyForm(ClaimsPrincipal principal)
    {
        return
        [
            new FormControlDefinition
            {
                Key = "name",
                Field = "name",
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(4)
                    .WithMaxLength(32)
                    .Build(),
            },
            new FormControlDefinition
            {
                Key = "key",
                Field = "key",
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(8)
                    .WithMaxLength(256)
                    .WithPattern(@"^[a-zA-Z0-9!\$%()*+,\-./:;<=>@\[\\\]^_`{|}~]+$")
                    .Build(),
            },
            new FormControlDefinition
            {
                Key = "roles",
                Field = "roles",
                Type = FormType.MultiText,
                ForceSingle = true,
                ValueType = FormValueType.String,
                Options = principal.Claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Select(r => FormControlOption.Option(r, r))
                    .ToList(),
            },
        ];
    }

    [GeneratedRegex(@"^[a-zA-Z0-9!\$%()*+,\-./:;<=>@\[\\\]^_`{|}~]+$")]
    private static partial Regex MyRegex();
}
