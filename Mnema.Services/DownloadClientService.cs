using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

public class DownloadClientService(ILogger<DownloadClientService> logger, IUnitOfWork unitOfWork, IServiceProvider serviceProvider): IDownloadClientService
{
    public async Task MarkAsFailed(Guid id, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.DownloadClientRepository.GetDownloadClientAsync(id, cancellationToken);
        if (client == null) return;

        client.IsFailed = true;
        client.FailedAt = DateTime.UtcNow;

        unitOfWork.DownloadClientRepository.Update(client);
        await unitOfWork.CommitAsync();

        BackgroundJob.Schedule(
            ()  => ReleaseFailedLock(id, CancellationToken.None),
            TimeSpan.FromHours(1));
    }

    public async Task ReleaseFailedLock(Guid id, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.DownloadClientRepository.GetDownloadClientAsync(id, cancellationToken);
        if (client is not { IsFailed: true }) return;

        logger.LogInformation("Releasing failed lock on Download client {Id} of type {Type}", client.Id, client.Type);

        client.IsFailed = false;
        client.FailedAt = null;

        unitOfWork.DownloadClientRepository.Update(client);
        await unitOfWork.CommitAsync();
    }

    public async Task UpdateDownloadClientAsync(DownloadClientDto dto, CancellationToken cancellationToken)
    {
        var configurationProvider = serviceProvider.GetKeyedService<IConfigurationProvider>(dto.Type);
        if (configurationProvider == null)
            throw new MnemaException($"Download client with type {dto.Type} cannot be configured");

        var client = await unitOfWork.DownloadClientRepository.GetDownloadClientAsync(dto.Id, cancellationToken);
        if (client == null)
        {
            var allowedTypes = await GetFreeTypesAsync(cancellationToken);
            if (!allowedTypes.Contains(dto.Type))
                throw new MnemaException($"Download client with type {dto.Type} has already been configured");

            client = new DownloadClient();
        }

        client.Name = dto.Name;
        client.Type = dto.Type;

        client.Metadata = new MetadataBag();
        foreach (var control in await configurationProvider.GetFormControls(cancellationToken))
        {
            if (dto.Metadata.TryGetValue(control.Key, out var value))
                client.Metadata[control.Key] = value;
        }

        if (client.Id.Equals(Guid.Empty))
        {
            unitOfWork.DownloadClientRepository.Add(client);
        }
        else
        {
            unitOfWork.DownloadClientRepository.Update(client);
        }

        await unitOfWork.CommitAsync();

        await configurationProvider.ReloadConfiguration(cancellationToken);
    }

    public async Task<List<DownloadClientType>> GetFreeTypesAsync(CancellationToken cancellationToken)
    {
        var inUseTypes = (await unitOfWork.DownloadClientRepository
            .GetInUseTypesAsync(cancellationToken)).ToHashSet();

        return Enum.GetValues<DownloadClientType>()
            .Where(c => !inUseTypes.Contains(c))
            .ToList();
    }

    public async Task<FormDefinition?> GetFormDefinitionForType(DownloadClientType type, CancellationToken cancellationToken)
    {
        var configurationProvider = serviceProvider.GetKeyedService<IConfigurationProvider>(type);
        if (configurationProvider == null)
            return null;

        var freeTypes = await GetFreeTypesAsync(cancellationToken);
        if (!freeTypes.Contains(type))
            freeTypes.Add(type);

        return new FormDefinition
        {
            Key = $"settings.download-clients.{type.ToString()}",
            Controls = [
                new FormControlDefinition
                {
                    Key = "name",
                    Field = "name",
                    Type = FormType.Text,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "type",
                    Field = "type",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = freeTypes
                        .Select(t => new FormControlOption(t.ToString().ToLower(), t))
                        .ToList()
                },
                ..await configurationProvider.GetFormControls(cancellationToken)
            ],
        };
    }
}
