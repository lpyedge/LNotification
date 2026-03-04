using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class MattermostProvider : NotificationProviderBase<MattermostProvider.MattermostConfig, MattermostProvider.MattermostSendOptions>
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

    public sealed class MattermostConfig : ProviderConfigBase, IProviderSendOptions<MattermostSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public MattermostSendOptions SendOptions { get; set; } = new();
    }

    internal MattermostProvider(
        IHttpClientFactory factory,
        ILogger<MattermostProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        MattermostConfig config,
        string message,
        MattermostSendOptions options)
    {
        var channel = options.Channel ?? config.Channel;

        var payload = new
        {
            channel = channel,
            text = message,
            username = options.Username,
            icon_url = options.IconUrl
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
