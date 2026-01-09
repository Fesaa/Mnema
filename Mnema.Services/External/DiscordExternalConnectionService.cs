using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API.External;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.External;

namespace Mnema.Services.External;

internal sealed record DiscordMessage
{
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
    public required DiscordEmbed[] Embeds { get; set; }
}

internal sealed record DiscordEmbed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public DateTime? Timestamp { get; set; }
    public int? Color { get; set; }
    public DiscordEmbedImage? Thumbnail { get; set; }
    public DiscordEmbedAuthor? Author { get; set; }
    public DiscordEmbedImage? Image { get; set; }
    public DiscordEmbedField[]? Fields { get; set; }
    public DiscordEmbedFooter? Footer { get; set; }
}

internal sealed record DiscordEmbedFooter
{
    public string? Text { get; set; }
    public string? IconUrl { get; set; }
    public string? ProxyIconUrl { get; set; }
}

internal sealed record DiscordEmbedImage
{
    public string? Url { get; set; }
    public string? ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
}

internal sealed record DiscordEmbedAuthor
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? IconUrl { get; set; }
    public string? ProxyIconUrl { get; set; }
}

internal sealed record DiscordEmbedField
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public bool? Inline { get; set; }
}

internal class DiscordExternalConnectionService(
    ILogger<DiscordExternalConnectionService> logger,
    HttpClient httpClient
) : IExternalConnectionHandlerService
{
    private const string WebhookKey = "webhook";
    private const string UsernameKey = "username";
    private const string AvatarKey = "avatar";

    private const int MaxDescriptionLength = 4096;

    private static readonly JsonSerializerOptions DiscordJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public List<ExternalConnectionEvent> SupportedEvents { get; } =
    [
        ExternalConnectionEvent.DownloadStarted,
        ExternalConnectionEvent.DownloadFinished,
        ExternalConnectionEvent.DownloadFailure
    ];

    public Task CommunicateDownloadStarted(ExternalConnection connection, DownloadInfo info)
    {
        var embed = new DiscordEmbed
        {
            Title = "Download Started",
            Description = $"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength),
            Color = 0x3498db, // Blue
            Timestamp = DateTime.UtcNow,
            Fields = BuildDefaultEmbedFields(info).ToArray(),
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {info.Id}"
            }
        };

        if (!string.IsNullOrEmpty(info.RefUrl)) embed.Url = info.RefUrl;

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.Image = new DiscordEmbedImage
            {
                Url = info.ImageUrl
            };

        return SendMessage(connection, [embed]);
    }

    public Task CommunicateDownloadFinished(ExternalConnection connection, DownloadInfo info)
    {
        var embed = new DiscordEmbed
        {
            Title = "Download Complete",
            Description = $"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength),
            Color = 0x2ecc71, // Green
            Timestamp = DateTime.UtcNow,
            Fields = BuildDefaultEmbedFields(info).ToArray(),
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {info.Id}"
            }
        };

        if (!string.IsNullOrEmpty(info.RefUrl)) embed.Url = info.RefUrl;

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.Image = new DiscordEmbedImage
            {
                Url = info.ImageUrl
            };

        return SendMessage(connection, [embed]);
    }

    private List<DiscordEmbedField> BuildDefaultEmbedFields(DownloadInfo info)
    {
        var embeds = new List<DiscordEmbedField>
        {
            new()
            {
                Name = "Provider",
                Value = info.Provider.ToString(),
                Inline = true
            },
            new()
            {
                Name = "Newly downloaded",
                Value = info.Size,
                Inline = true
            },
            new()
            {
                Name  = "Total available",
                Value = info.TotalSize,
                Inline = true
            },
            new()
            {
                Name = "Location",
                Value = $"`{info.DownloadDir}`",
                Inline = false
            }
        };


        return embeds;
    }

    public Task CommunicateDownloadFailure(ExternalConnection connection, DownloadInfo info, Exception ex)
    {
        var progressText = info.Progress > 0
            ? $"{info.Progress:F1}% complete before failure"
            : "Failed before download started";

        var embed = new DiscordEmbed
        {
            Title = "Download Failed",
            Description = $"**{info.Name}**\n\n{ex.StackTrace}".Limit(MaxDescriptionLength),
            Color = 0xe74c3c, // Red
            Timestamp = DateTime.UtcNow,
            Fields =
            [
                new DiscordEmbedField
                {
                    Name = "Provider",
                    Value = info.Provider.ToString(),
                    Inline = true
                },
            ],
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {info.Id}"
            }
        };

        if (!string.IsNullOrEmpty(info.RefUrl)) embed.Url = info.RefUrl;

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.Image = new DiscordEmbedImage
            {
                Url = info.ImageUrl
            };

        return SendMessage(connection, [embed]);
    }


    public Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = WebhookKey,
                Type = FormType.Text,
                ForceSingle = true,
                Validators = new FormValidatorsBuilder()
                    .WithStartsWith("https://discord.com/api/webhooks/")
                    .Build()
            },
            new FormControlDefinition
            {
                Key = UsernameKey,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = AvatarKey,
                Type = FormType.Text
            }
        ]);
    }

    private async Task SendMessage(ExternalConnection connection, DiscordEmbed[] embeds)
    {
        var url = connection.Metadata.GetString(WebhookKey);
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("No webhook URL provided for connection {Guid}, cannot send message", connection.Id);
            return;
        }

        var message = new DiscordMessage
        {
            Username = connection.Metadata.GetStringOrDefault(UsernameKey, null),
            AvatarUrl = connection.Metadata.GetStringOrDefault(AvatarKey, null),
            Embeds = embeds
        };

        var json = JsonSerializer.Serialize(message, DiscordJsonSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
    }
}
