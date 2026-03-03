using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class SlackProvider : NotificationProviderBase
{
    /// <summary>
    /// Per-message options for Slack webhook notifications.
    /// </summary>
    public sealed class SlackSendOptions : SendOptions
    {
        /// <summary>Override the target channel (e.g. "#alerts", "#general").</summary>
        public string? Channel { get; set; }

        /// <summary>Override the bot display name for this message.</summary>
        public string? Username { get; set; }

        /// <summary>Bot icon as Slack emoji shortcode (e.g. ":robot_face:", ":warning:").</summary>
        public string? IconEmoji { get; set; }
    }

    public sealed class SlackConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    internal SlackProvider(
        IHttpClientFactory factory,
        ILogger<SlackProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (SlackConfig)config;
        var o = options as SlackSendOptions;

        var payload = new
        {
            text = $"{Emoji(level)} {message}",
            channel = o?.Channel,
            username = o?.Username,
            icon_emoji = o?.IconEmoji
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
        var c = (SlackConfig)config;
        var o = options as SlackSendOptions;

        var payload = new
        {
            text = $"{Emoji(level)} {markdownContent}",
            mrkdwn = true,
            channel = o?.Channel,
            username = o?.Username,
            icon_emoji = o?.IconEmoji
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
