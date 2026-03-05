using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class SlackProvider : NotificationProviderBase<SlackProvider.SlackConfig, SlackProvider.SlackSendOptions>
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
        
        /// <summary>Bot avatar image URL. If set, sent as `icon_url` to Slack.</summary>
        public string? AvatarUrl { get; set; }
    }

    public sealed class SlackConfig : ProviderConfigBase, IProviderSendOptions<SlackSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public SlackSendOptions SendOptions { get; set; } = new();
    }

    internal SlackProvider(
        IHttpClientFactory factory,
        ILogger<SlackProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        SlackConfig config,
        string message,
        SlackSendOptions options)
    {
        var payload = new
        {
            text = message,
            mrkdwn = options.ContentFormat == MessageContentFormat.Markdown,
            channel = options.Channel,
            username = options.Username,
            icon_emoji = options.IconEmoji,
            icon_url = options.AvatarUrl
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        using var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
