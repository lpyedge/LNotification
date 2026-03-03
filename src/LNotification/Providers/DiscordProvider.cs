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

public sealed class DiscordProvider : NotificationProviderBase
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

    public sealed class DiscordConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    internal DiscordProvider(
        IHttpClientFactory factory,
        ILogger<DiscordProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (DiscordConfig)config;
        var o = options as DiscordSendOptions;

        var payload = new
        {
            content = $"{Emoji(level)} {message}",
            username = o?.Username,
            avatar_url = o?.AvatarUrl,
            flags = o != null && o.Flags != DiscordMessageFlag.None ? (int?)o.Flags : null
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        // Discord natively supports Markdown
        var c = (DiscordConfig)config;
        var o = options as DiscordSendOptions;

        var payload = new
        {
            content = $"{Emoji(level)} {markdownContent}",
            username = o?.Username,
            avatar_url = o?.AvatarUrl,
            flags = o != null && o.Flags != DiscordMessageFlag.None ? (int?)o.Flags : null
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
