using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class MattermostProvider : NotificationProviderBase
{
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
        NotificationService.NotifyLevel level)
    {
        var c = (MattermostConfig)config;
        object payload;

        if (!string.IsNullOrWhiteSpace(c.Channel))
        {
            payload = new
            {
                channel = c.Channel,
                text = $"{Emoji(level)} {message}"
            };
        }
        else
        {
            payload = new
            {
                text = $"{Emoji(level)} {message}"
            };
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        // Mattermost natively supports Markdown
        var c = (MattermostConfig)config;
        object payload;

        if (!string.IsNullOrWhiteSpace(c.Channel))
        {
            payload = new
            {
                channel = c.Channel,
                text = $"{Emoji(level)} {markdownContent}"
            };
        }
        else
        {
            payload = new
            {
                text = $"{Emoji(level)} {markdownContent}"
            };
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
