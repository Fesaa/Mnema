using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.External;
using Mnema.Models.Internal;

namespace Mnema.Services.External;

internal sealed record ScanFolderDto
{
    public required string ApiKey { get; init; }
    public required string FolderPath { get; set; }
    public bool AbortOnNoSeriesMatch { get; set; } = true;
}

internal class KavitaExternalConnectionService(
    ILogger<KavitaExternalConnectionService> logger,
    HttpClient httpClient,
    ApplicationConfiguration applicationConfiguration
) : IExternalConnectionHandlerService
{
    private const string ApiKey = "api-key";
    private const string UrlKey = "url";
    private const string BaseDirKey = "basedir";

    public List<ExternalConnectionEvent> SupportedEvents { get; } = [ExternalConnectionEvent.DownloadFinished];

    public Task CommunicateDownloadStarted(ExternalConnection connection, DownloadInfo info)
    {
        throw new NotImplementedException();
    }

    public async Task CommunicateDownloadFinished(ExternalConnection connection, DownloadInfo info)
    {
        var url = connection.Metadata.GetString(UrlKey);
        var authKey = connection.Metadata.GetString(ApiKey);
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(authKey))
        {
            logger.LogWarning("Kavita url or auth key is empty, but connection is registered. Cannot communicate");
            return;
        }

        var baseDirOverride = connection.Metadata.GetString(BaseDirKey);
        var baseDir = string.IsNullOrEmpty(baseDirOverride) ? applicationConfiguration.BaseDir : baseDirOverride;

        var dto = new ScanFolderDto
        {
            ApiKey = authKey,
            FolderPath = Path.Join(baseDir, info.DownloadDir),
            AbortOnNoSeriesMatch = true
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var req = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/api/Library/scan-folder");
        req.Content = content;
        req.Headers.Add(Headers.KavitaAuthKey, authKey);

        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();
    }

    public Task CommunicateDownloadFailure(ExternalConnection connection, DownloadInfo info, Exception ex)
    {
        throw new NotImplementedException();
    }

    public Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = ApiKey,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithRequired()
                    .WithMinLength(8)
                    .WithMaxLength(32)
                    .Build()
            },
            new FormControlDefinition
            {
                Key = UrlKey,
                Type = FormType.Text,
                Validators = new FormValidatorsBuilder()
                    .WithIsUrl()
                    .Build()
            },
            new FormControlDefinition
            {
                Key = BaseDirKey,
                Type = FormType.Text
            },
        ]);
    }
}
