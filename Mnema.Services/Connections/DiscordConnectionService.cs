using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;

namespace Mnema.Services.Connections;

internal class DiscordConnectionService(
    ILogger<DiscordConnectionService> logger,
    HttpClient httpClient,
    ApplicationConfiguration applicationConfiguration
) : AbstractConnectionHandlerService
{
    private static readonly IMetadataKey<string?> WebhookKey = MetadataKeys.OptionalString("webhook");
    private static readonly IMetadataKey<string?> UsernameKey = MetadataKeys.OptionalString("username");
    private static readonly IMetadataKey<string?> AvatarKey = MetadataKeys.OptionalString("avatar");

    private const int MaxDescriptionLength = 4096;

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

    public override Task CommunicateDownloadStarted(Connection connection, DownloadInfo info)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Download Started")
            .WithDescription($"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength))
            .WithColor(0x3498db) // Blue
            .WithTimestamp(DateTime.UtcNow)
            .WithFields(BuildDefaultEmbedFields(info))
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {info.Id}"));

        if (!string.IsNullOrEmpty(info.RefUrl))
            embed.WithUrl(info.RefUrl);

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.WithImageUrl(info.ImageUrl);

        return SendMessage(connection, [embed.Build()]);
    }

    public override Task CommunicateDownloadFinished(Connection connection, DownloadInfo info)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Download Complete")
            .WithDescription($"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength))
            .WithColor(0x2ecc71) // Green
            .WithTimestamp(DateTime.UtcNow)
            .WithFields(BuildDefaultEmbedFields(info))
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {info.Id}"));

        if (!string.IsNullOrEmpty(info.RefUrl))
            embed.WithUrl(info.RefUrl);

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.WithImageUrl(info.ImageUrl);

        return SendMessage(connection, [embed.Build()]);
    }

    public override Task CommunicateSubscriptionExhausted(Connection connection, DownloadInfo info)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Series fully downloaded")
            .WithDescription($"**{info.Name}**\n\n{info.Description}".Limit(MaxDescriptionLength))
            .WithColor(0xf1c40f) // Yellow
            .WithTimestamp(DateTime.UtcNow)
            .WithFields(BuildDefaultEmbedFields(info))
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {info.Id}"));

        if (!string.IsNullOrEmpty(info.RefUrl))
            embed.WithUrl(info.RefUrl);

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.WithImageUrl(info.ImageUrl);

        return SendMessage(connection, [embed.Build()], MonitoredSeriesComponents(info.MonitoredSeriesId));
    }

    public override Task CommunicateSeriesMonitored(Connection connection, MonitoredSeries series)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Series monitored")
            .WithDescription($"**{series.Title}**\n\n{series.Summary}".Limit(MaxDescriptionLength))
            .WithColor(0x1F8B4C)
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {series.Id}"));

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.WithUrl(series.RefUrl);

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.WithImageUrl(series.CoverUrl);

        return SendMessage(connection, [embed.Build()], MonitoredSeriesComponents(series.Id));
    }

    public override Task CommunicateSeriesUnmonitored(Connection connection, MonitoredSeries series)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Series unmonitored")
            .WithDescription($"**{series.Title}**\n\n{series.Summary}".Limit(MaxDescriptionLength))
            .WithColor(0xED4245)
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {series.Id}"));

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.WithUrl(series.RefUrl);

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.WithImageUrl(series.CoverUrl);

        return SendMessage(connection, [embed.Build()]);
    }

    public override Task CommunicateTooManyForAutomatedDownload(Connection connection, MonitoredSeries series, int amount)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Manual intervention required")
            .WithDescription(
                $"Cannot automatically start download for {series.Title} as it wants to download {amount} chapters at once.")
            .WithColor(0xE67E22)
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {series.Id}"));

        if (!string.IsNullOrEmpty(series.RefUrl))
            embed.WithUrl(series.RefUrl);

        if (!string.IsNullOrEmpty(series.CoverUrl) && series.CoverUrl.StartsWith("http"))
            embed.WithImageUrl(series.CoverUrl);

        if (string.IsNullOrEmpty(applicationConfiguration.Host))
            return SendMessage(connection, [embed.Build()]);

        return SendMessage(connection, [embed.Build()], new ComponentBuilder()
            .WithButton(new ButtonBuilder()
                .WithLabel("Open Downloads")
                .WithUrl($"{applicationConfiguration.Host}/active-downloads")
                .WithStyle(ButtonStyle.Link))
            .Build());
    }

    public override Task CommunicateDownloadClientEvent(Connection connection, DownloadClient client)
    {
        var colour = client.IsFailed ? (uint)0xe74c3c : 0x2ecc71;

        var embed = new EmbedBuilder()
            .WithTitle(client.IsFailed ? "Download client locked" : "Download client unlocked")
            .WithDescription(client.IsFailed
                ? $"Client {client.Name} is unreachable and is locked until {client.FailedAt?.AddHours(1)}"
                : $"Client {client.Name} is reachable again and has been unlocked")
            .WithColor(colour)
            .WithTimestamp(DateTime.UtcNow)
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {client.Id}"))
            .Build();

        if (string.IsNullOrEmpty(applicationConfiguration.Host))
            return SendMessage(connection, [embed]);

        var component = new ComponentBuilder()
            .WithButton(new ButtonBuilder()
                .WithLabel("Manage Download Clients")
                .WithUrl($"{applicationConfiguration.Host}/settings#download_clients")
                .WithStyle(ButtonStyle.Link));

        return SendMessage(connection, [embed], component.Build());
    }

    private MessageComponent? MonitoredSeriesComponents(Guid? id)
    {
        if (string.IsNullOrEmpty(applicationConfiguration.Host) || id == null) return null;

        return new ComponentBuilder()
            .WithButton(new ButtonBuilder()
                .WithLabel("Manage Monitored Series")
                .WithUrl($"{applicationConfiguration.Host}/monitored-series-detail/{id}")
                .WithStyle(ButtonStyle.Link))
            .Build();
    }

    private static List<EmbedFieldBuilder> BuildDefaultEmbedFields(DownloadInfo info)
    {
        var fields = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName("Provider")
                .WithValue(info.Provider.ToString())
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName("Newly downloaded")
                .WithValue(info.Size)
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName("Total available")
                .WithValue(info.TotalSize)
                .WithIsInline(true),
            new EmbedFieldBuilder()
                .WithName("Location")
                .WithValue($"`{info.DownloadDir}`")
                .WithIsInline(false),
        };

        if (!string.IsNullOrEmpty(info.ReDownloadSize))
        {
            fields.Add(new EmbedFieldBuilder()
                .WithName("Re-download")
                .WithValue(info.ReDownloadSize)
                .WithIsInline(true));
        }

        return fields;
    }

    public override Task CommunicateDownloadFailure(Connection connection, DownloadInfo info, Exception ex)
    {
        var progressText = info.Progress > 0
            ? $"{info.Progress:F1}% complete before failure"
            : "Failed before download started";

        var embed = new EmbedBuilder()
            .WithTitle("Download Failed")
            .WithDescription($"**{info.Name}**\n\n{ex.StackTrace}".Limit(MaxDescriptionLength))
            .WithColor(0xe74c3c) // Red
            .WithTimestamp(DateTime.UtcNow)
            .WithFields(
                new EmbedFieldBuilder()
                    .WithName("Provider")
                    .WithValue(info.Provider.ToString())
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Progress")
                    .WithValue(progressText)
                    .WithIsInline(true))
            .WithFooter(new EmbedFooterBuilder().WithText($"ID: {info.Id}"));

        if (!string.IsNullOrEmpty(info.RefUrl))
            embed.WithUrl(info.RefUrl);

        if (!string.IsNullOrEmpty(info.ImageUrl))
            embed.WithImageUrl(info.ImageUrl);

        return SendMessage(connection, [embed.Build()]);
    }

    public override Task CommunicateException(Connection connection, string message, Exception ex)
    {
        var embed = new EmbedBuilder()
            .WithTitle("An exception occurred!")
            .WithDescription($"**{message}**\n\n{ex.StackTrace}".Limit(MaxDescriptionLength))
            .WithColor(0xe74c3c) // Red
            .WithTimestamp(DateTime.UtcNow)
            .WithFields(
                new EmbedFieldBuilder()
                    .WithName("Exception")
                    .WithValue(ex.Message)
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Source")
                    .WithValue(ex.Source ?? "N/A")
                    .WithIsInline(true),
                new EmbedFieldBuilder()
                    .WithName("Type")
                    .WithValue(ex.GetType().FullName ?? "N/A")
                    .WithIsInline(false))
            .Build();

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

    private async Task SendMessage(Connection connection, Embed[] embeds, MessageComponent? components = null)
    {
        var url = connection.Metadata.GetKey(WebhookKey);
        if (string.IsNullOrEmpty(url))
        {
            logger.LogWarning("No webhook URL provided for connection {Guid}, cannot send message", connection.Id);
            return;
        }

        using var client = new DiscordWebhookClient(url);

        try
        {
            await client.SendMessageAsync(embeds: embeds, components: components);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to Discord webhook trying again in 1s");
            await Task.Delay(1000);

            await client.SendMessageAsync(embeds: embeds, components: components);
        }
    }
}
