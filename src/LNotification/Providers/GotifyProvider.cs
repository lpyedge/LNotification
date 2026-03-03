using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class GotifyProvider : NotificationProviderBase
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

    public sealed class GotifyConfig : ProviderConfigBase
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int Priority { get; set; } = 5;
    }

    internal GotifyProvider(
        IHttpClientFactory factory,
        ILogger<GotifyProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (GotifyConfig)config;
        var o = options as GotifySendOptions;
        var url = $"{c.ServerUrl.TrimEnd('/')}/message?token={c.Token}";
        var payload = new
        {
            title = o?.Title ?? $"{Emoji(level)} [{level}]",
            message = message,
            priority = o?.Priority ?? c.Priority
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (GotifyConfig)config;
        var o = options as GotifySendOptions;
        var url = $"{c.ServerUrl.TrimEnd('/')}/message?token={c.Token}";
        var payload = new
        {
            title = o?.Title ?? $"{Emoji(level)} [{level}]",
            message = markdownContent,
            priority = o?.Priority ?? c.Priority,
            extras = new
            {
                display = new { contentType = "text/markdown" }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
