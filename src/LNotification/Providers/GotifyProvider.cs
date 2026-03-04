using System;
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
        /// <summary>notification title.</summary>
        public string Title { get; set; } = "Notification";
        /// <summary>Override message priority (1-5, default 3).</summary>
        public int? Priority { get; set; } = 3;
    }

    public sealed class GotifyConfig : ProviderConfigBase, IProviderSendOptions<GotifySendOptions>
    {
        public string ServerUrl { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
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
        GotifySendOptions options)
    {
        var url = $"{config.ServerUrl.TrimEnd('/')}/message?token={config.Token}";
        var mapped = MapGotifyPriority(options.Priority);

        var payload = new
        {
            title = options.Title ?? "Notification",
            message = message,
            priority = mapped,
            extras = options.ContentFormat == MessageContentFormat.Markdown
                ? new { display = new { contentType = "text/markdown" } }
                : null
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }

    //1-5 mapped linearly to 0-10 for Gotify priority
    private static int MapGotifyPriority(int? p)
    {
        // Expect p in 1..5, map linearly to 0..10
        int priority = p ?? 3;
        if (priority < 1) priority = 1;
        if (priority > 5) priority = 5;
        return (int)Math.Round((priority - 1) * 10.0 / 4.0);
    }
}
