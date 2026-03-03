using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class GotifyProvider : NotificationProviderBase<GotifyProvider.GotifyConfig, GotifyProvider.GotifySendOptions>
{
    /// <summary>
    /// Per-message options for Gotify notifications.
    /// </summary>
    public sealed class GotifySendOptions : SendOptions
    {
        /// <summary>Custom notification title. Overrides default "[emoji] [level]".</summary>
        public string? Title { get; set; }

        /// <summary>Override message priority (0 = min, 10 = max).
        /// 0: no notification. 1-3: low. 4-7: normal. 8-10: high.</summary>
        public int? Priority { get; set; }
    }

    public sealed class GotifyConfig : ProviderConfigBase, IProviderSendOptions<GotifySendOptions>
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int Priority { get; set; } = 5;
        public GotifySendOptions SendOptions { get; set; } = new();
    }

    internal GotifyProvider(
        IHttpClientFactory factory,
        ILogger<GotifyProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        GotifyConfig config,
        string message,
        NotificationService.NotifyLevel level,
        GotifySendOptions options)
    {
        var url = $"{config.ServerUrl.TrimEnd('/')}/message?token={config.Token}";
        var payload = new
        {
            title = options.Title ?? $"{Emoji(level)} [{level}]",
            message = message,
            priority = options.Priority ?? config.Priority,
            extras = options.ContentFormat == MessageContentFormat.Markdown
                ? new { display = new { contentType = "text/markdown" } }
                : null
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
