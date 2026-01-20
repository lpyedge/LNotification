using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class FeishuProvider : NotificationProviderBase
{
    public sealed class FeishuConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    internal FeishuProvider(
        IHttpClientFactory factory,
        ILogger<FeishuProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (FeishuConfig)config;
        var payload = new
        {
            msg_type = "text",
            content = new
            {
                text = $"{Emoji(level)} {message}"
            }
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
        var plain = RegexPatterns.StripMarkdown(markdownContent);
        return SendInternalAsync(config, plain, level);
    }
}
