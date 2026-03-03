using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class GotifyProvider : NotificationProviderBase
{
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
        NotificationService.NotifyLevel level)
    {
        var c = (GotifyConfig)config;
        var url = $"{c.ServerUrl.TrimEnd('/')}/message?token={c.Token}";
        var payload = new
        {
            title = $"{Emoji(level)} [{level}]",
            message = message,
            priority = c.Priority
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var c = (GotifyConfig)config;
        var url = $"{c.ServerUrl.TrimEnd('/')}/message?token={c.Token}";
        var payload = new
        {
            title = $"{Emoji(level)} [{level}]",
            message = markdownContent,
            priority = c.Priority,
            extras = new
            {
                // Gotify supports markdown via client::display content type
                display = new { contentType = "text/markdown" }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
