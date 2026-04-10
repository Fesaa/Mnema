using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Connections;

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

    public DiscordEmbedImage() {}

    public DiscordEmbedImage(string url)
    {
        Url = url;
    }
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

internal class DiscordConnectionService(
    ILogger<DiscordConnectionService> logger,
    HttpClient httpClient
) : AbstractConnectionHandlerService
{
    private static readonly IMetadataKey<string?> WebhookKey = MetadataKeys.OptionalString("webhook");
    private static readonly IMetadataKey<string?> UsernameKey = MetadataKeys.OptionalString("username");
    private static readonly IMetadataKey<string?> AvatarKey = MetadataKeys.OptionalString("avatar");

    private const int MaxDescriptionLength = 4096;

    private static readonly JsonSerializerOptions DiscordJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public override List<ConnectionEvent> SupportedEvents { get; } =
    [
        ConnectionEvent.DownloadStarted,
        ConnectionEvent.DownloadFinished,
        ConnectionEvent.DownloadFailure,
        ConnectionEvent.SubscriptionExhausted,
        ConnectionEvent.SeriesMonitored,
        ConnectionEvent.SeriesUnmonitored,
        ConnectionEvent.TooManyForAutomatedDownload,
        ConnectionEvent.DownloadClientEvents,
        ConnectionEvent.Exception,
    ];

    public new Task CommunicateDownloadStarted(Connection connection, DownloadInfo info)
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
            embed.Image = new DiscordEmbedImage(info.ImageUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateDownloadFinished(Connection connection, DownloadInfo info)
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
            embed.Image = new DiscordEmbedImage(info.ImageUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateSubscriptionExhausted(Connection connection, DownloadInfo info)
    {
        var embed = new DiscordEmbed
        {
            Title = "Series fully downloaded",
            Description = $"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength),
            Color = 0xf1c40f, // Yellow
            Timestamp = DateTime.UtcNow,
            Fields = BuildDefaultEmbedFields(info).ToArray(),
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {info.Id}"
            }
        };

        if (!string.IsNullOrEmpty(info.RefUrl)) embed.Url = info.RefUrl;

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.Image = new DiscordEmbedImage(info.ImageUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateSeriesMonitored(Connection connection, MonitoredSeries series)
    {
        var embed = new DiscordEmbed
        {
            Title = "Series monitored",
            Description = $"**{series.Title}**\n\n{series.Summary}".Limit(MaxDescriptionLength),
            Color = 0x1F8B4C,
            Timestamp = DateTime.UtcNow,
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {series.Id}"
            }
        };

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.Url = series.RefUrl;

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.Image = new DiscordEmbedImage(series.CoverUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateSeriesUnmonitored(Connection connection, MonitoredSeries series)
    {
        var embed = new DiscordEmbed
        {
            Title = "Series unmonitored",
            Description = $"**{series.Title}**\n\n{series.Summary}".Limit(MaxDescriptionLength),
            Color = 0xED4245,
            Timestamp = DateTime.UtcNow,
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {series.Id}"
            }
        };

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.Url = series.RefUrl;

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.Image = new DiscordEmbedImage(series.CoverUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateTooManyForAutomatedDownload(Connection connection, MonitoredSeries series, int amount)
    {
        var embed = new DiscordEmbed
        {
            Title = "Manual intervention required",
            Description =
                $"Cannot automatically start download for {series.Title} as it wants to download {amount} chapters at once.",
            Color = 0xE67E22,
            Timestamp = DateTime.UtcNow,
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {series.Id}"
            }
        };

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.Url = series.RefUrl;

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.Image = new DiscordEmbedImage(series.CoverUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateDownloadClientEvent(Connection connection, DownloadClient client)
    {
        var embed = new DiscordEmbed
        {
            Title = client.IsFailed ? "Download client locked" : "Download client unlocked",
            Description = client.IsFailed ? $"Client {client.Name} is unreachable and is locked until {client.FailedAt?.AddHours(1)}"
                : $"Client {client.Name} is reachable again and has been unlocked",
            Color = client.IsFailed ? 0xe74c3c : 0x2ecc71,
            Timestamp = DateTime.UtcNow,
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {client.Id}"
            }
        };

        return SendMessage(connection, [embed]);
    }

    private static List<DiscordEmbedField> BuildDefaultEmbedFields(DownloadInfo info)
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

        if (!string.IsNullOrEmpty(info.ReDownloadSize))
        {
            embeds.Add(new()
            {
                Name = "Re-download",
                Value = info.ReDownloadSize,
                Inline = true
            });
        }

        return embeds;
    }

    public new Task CommunicateDownloadFailure(Connection connection, DownloadInfo info, Exception ex)
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
                new DiscordEmbedField()
                {
                    Name = "Progress",
                    Value = progressText,
                    Inline = true
                }
            ],
            Footer = new DiscordEmbedFooter
            {
                Text = $"ID: {info.Id}"
            }
        };

        if (!string.IsNullOrEmpty(info.RefUrl)) embed.Url = info.RefUrl;

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.Image = new DiscordEmbedImage(info.ImageUrl);

        return SendMessage(connection, [embed]);
    }

    public new Task CommunicateException(Connection connection, string message, Exception ex)
    {
        var embed = new DiscordEmbed
        {
            Title = "An exceptions occured!",
            Description = $"**{message}**\n\n{ex.StackTrace}".Limit(MaxDescriptionLength),
            Color = 0xe74c3c, // Red
            Timestamp = DateTime.UtcNow,
            Fields = [
                new DiscordEmbedField()
                {
                    Name = "Exception",
                    Value = ex.Message,
                    Inline = true
                },
                new DiscordEmbedField
                {
                    Name = "Source",
                    Value = ex.Source ?? "N/A",
                    Inline = true
                },
                new DiscordEmbedField
                {
                    Name = "Type",
                    Value = ex.GetType().FullName ?? "N/A",
                    Inline = false
                },
            ]
        };

        return SendMessage(connection, [embed]);
    }


    public override Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult<List<FormControlDefinition>>([
            new FormControlDefinition
            {
                Key = WebhookKey.Key,
                Type = FormType.Text,
                ForceSingle = true,
                Validators = new FormValidatorsBuilder()
                    .WithStartsWith("https://discord.com/api/webhooks/")
                    .Build()
            },
            new FormControlDefinition
            {
                Key = UsernameKey.Key,
                Type = FormType.Text
            },
            new FormControlDefinition
            {
                Key = AvatarKey.Key,
                Type = FormType.Text
            }
        ]);
    }

    private async Task SendMessage(Connection connection, DiscordEmbed[] embeds)
    {
        var url = connection.Metadata.GetKey(WebhookKey);
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("No webhook URL provided for connection {Guid}, cannot send message", connection.Id);
            return;
        }

        var message = new DiscordMessage
        {
            Username = connection.Metadata.GetKey(UsernameKey),
            AvatarUrl = connection.Metadata.GetKey(AvatarKey),
            Embeds = embeds
        };

        var json = JsonSerializer.Serialize(message, DiscordJsonSerializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
            return;

        // Retry after 1s if the first attempt failed
        await Task.Delay(1000);

        response = await httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

    }
}
