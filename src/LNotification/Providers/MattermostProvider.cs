using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class MattermostProvider : NotificationProviderBase
{
    /// <summary>
    /// Per-message options for Mattermost webhook notifications.
    /// </summary>
    public sealed class MattermostSendOptions : SendOptions
    {
        /// <summary>Override the webhook bot display name for this message.</summary>
        public string? Username { get; set; }

        /// <summary>Override the webhook bot avatar image URL.</summary>
        public string? IconUrl { get; set; }

        /// <summary>Override target channel (e.g. "town-square", "off-topic").</summary>
        public string? Channel { get; set; }
    }

    public sealed class MattermostConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public string? Channel { get; set; }
    }

    internal MattermostProvider(
        IHttpClientFactory factory,
        ILogger<MattermostProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (MattermostConfig)config;
        var o = options as MattermostSendOptions;
        var channel = o?.Channel ?? c.Channel;

        var payload = new
        {
            channel = channel,
            text = $"{Emoji(level)} {message}",
            username = o?.Username,
            icon_url = o?.IconUrl
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        // Mattermost natively supports Markdown
        var c = (MattermostConfig)config;
        var o = options as MattermostSendOptions;
        var channel = o?.Channel ?? c.Channel;

        var payload = new
        {
            channel = channel,
            text = $"{Emoji(level)} {markdownContent}",
            username = o?.Username,
            icon_url = o?.IconUrl
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
