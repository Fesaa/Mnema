using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Services.Connections;

internal sealed record ScanFolderDto
{
    public required string ApiKey { get; init; }
    public required string FolderPath { get; set; }
    public bool AbortOnNoSeriesMatch { get; set; } = true;
}

internal sealed record BaseDirMapping
{
    public required string Src { get; init; }
    public required string Dest { get; init; }
}

internal class KavitaConnectionService(
    ILogger<KavitaConnectionService> logger,
    HttpClient httpClient,
    ApplicationConfiguration applicationConfiguration
) : AbstractConnectionHandlerService
{
    private static readonly IMetadataKey<string> ApiKey = MetadataKeys.String("api-key");
    private static readonly IMetadataKey<string> UrlKey = MetadataKeys.String("url");
    private static readonly IMetadataKey<List<BaseDirMapping>> BaseDirMappings = MetadataKeys.JsonArray<BaseDirMapping>("basedir-mappings");
    private static readonly IMetadataKey<bool> AbortOnNoSeriesMatch = MetadataKeys.Bool(nameof(AbortOnNoSeriesMatch), true);

    public override List<ConnectionEvent> SupportedEvents { get; } = [ConnectionEvent.DownloadFinished];

    public override async Task CommunicateDownloadFinished(Connection connection, DownloadInfo info)
    {
        var url = connection.Metadata.GetKey(UrlKey);
        var authKey = connection.Metadata.GetKey(ApiKey);
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(authKey))
        {
            logger.LogWarning("Kavita url or auth key is empty, but connection is registered. Cannot communicate");
            return;
        }

        var baseDirMappings = connection.Metadata.GetKey(BaseDirMappings);

        var baseDir = Path.Join(applicationConfiguration.BaseDir, info.DownloadDir);

        // run contains on longest first in case of inclusions
        var mapping = baseDirMappings
            .OrderByDescending(m => m.Src.Length)
            .FirstOrDefault(m => !string.IsNullOrEmpty(m.Src) && !string.IsNullOrEmpty(m.Dest) && baseDir.Contains(m.Src));

        if (mapping is not null)
            baseDir = baseDir.Replace(mapping.Src, mapping.Dest);

        if (baseDir.Contains(".."))
        {
            logger.LogWarning("Skipping scan request for {BaseDir} as Kavita will reject the request", baseDir);
            return;
        }

        logger.LogDebug("Sending ScanFolder request for {BaseDir} for connection {ConnectId}", baseDir, connection.Id);

        var dto = new ScanFolderDto
        {
            ApiKey = authKey,
            FolderPath = baseDir,
            AbortOnNoSeriesMatch = connection.Metadata.GetKey(AbortOnNoSeriesMatch)
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var req = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/api/Library/scan-folder");
        req.Content = content;
        req.Headers.Add(Headers.KavitaAuthKey, authKey);

        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }

    public override Task<List<FormFieldDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormFieldDefinition>>([
            new TextFieldDefinition
            {
                Key = ApiKey.Key,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(8)
                    .WithMaxLength(32)
                    .Build()
            },
            new SwitchFieldDefinition
            {
                Key = AbortOnNoSeriesMatch.Key,
            },
            new TextFieldDefinition
            {
                Key = UrlKey.Key,
                Validators = new FormValidatorsBuilder()
                    .WithIsUrl()
                    .Build()
            },
            new ArrayFieldDefinition
            {
                Key = BaseDirMappings.Key,
                Controls =
                [
                    new TextFieldDefinition
                    {
                        Key = nameof(BaseDirMapping.Src),
                        Field = nameof(BaseDirMapping.Src),
                        Validators = FormValidatorsBuilder.Required
                    },
                    new TextFieldDefinition
                    {
                        Key = nameof(BaseDirMapping.Dest),
                        Field = nameof(BaseDirMapping.Dest),
                        Validators = FormValidatorsBuilder.Required
                    }
                ]
            },
        ]);
    }
}
