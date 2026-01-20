using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class SlackProvider : NotificationProviderBase
{
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
        NotificationService.NotifyLevel level)
    {
        var c = (SlackConfig)config;
        var payload = new
        {
            text = $"{Emoji(level)} {message}"
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient.Name);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var c = (SlackConfig)config;
        var payload = new
        {
            text = $"{Emoji(level)} {markdownContent}",
            mrkdwn = true
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient.Name);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
