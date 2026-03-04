using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Discord webhook message flags.</summary>
[Flags]
public enum DiscordMessageFlag
{
    /// <summary>No special flags.</summary>
    None = 0,
    /// <summary>Do not include any embeds when serializing this message.</summary>
    SuppressEmbeds = 1 << 2,
    /// <summary>This message will not trigger push or desktop notifications.</summary>
    SuppressNotifications = 1 << 12
}

public sealed class DiscordProvider : NotificationProviderBase<DiscordProvider.DiscordConfig, DiscordProvider.DiscordSendOptions>
{
    /// <summary>
    /// Per-message options for Discord webhook notifications.
    /// </summary>
    public sealed class DiscordSendOptions : SendOptions
    {
        /// <summary>Override the webhook bot display name for this message.</summary>
        public string? Username { get; set; }

        /// <summary>Override the webhook bot avatar image URL for this message.</summary>
        public string? AvatarUrl { get; set; }

        /// <summary>Message behavior flags (suppress embeds, suppress notifications).</summary>
        public DiscordMessageFlag Flags { get; set; } = DiscordMessageFlag.None;
    }

    public sealed class DiscordConfig : ProviderConfigBase, IProviderSendOptions<DiscordSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public DiscordSendOptions SendOptions { get; set; } = new();
    }

    internal DiscordProvider(
        IHttpClientFactory factory,
        ILogger<DiscordProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        DiscordConfig config,
        string message,
        DiscordSendOptions options)
    {
        var payload = new
        {
            content = message,
            username = options.Username,
            avatar_url = options.AvatarUrl,
            flags = options.Flags != DiscordMessageFlag.None ? (int?)options.Flags : null
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
