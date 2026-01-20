using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class DiscordProvider : NotificationProviderBase
{
    public sealed class DiscordConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    internal DiscordProvider(
        IHttpClientFactory factory,
        ILogger<DiscordProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (DiscordConfig)config;
        var payload = new
        {
            content = $"{Emoji(level)} {message}"
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient.Name);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        // Discord原生支持Markdown
        return SendInternalAsync(config, markdownContent, level);
    }
}
