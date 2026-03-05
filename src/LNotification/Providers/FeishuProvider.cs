using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class FeishuProvider : NotificationProviderBase<FeishuProvider.FeishuConfig, FeishuProvider.FeishuSendOptions>
{
    public sealed class FeishuSendOptions : SendOptions
    {
    }

    public sealed class FeishuConfig : ProviderConfigBase, IProviderSendOptions<FeishuSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public FeishuSendOptions SendOptions { get; set; } = new();
    }

    internal FeishuProvider(
        IHttpClientFactory factory,
        ILogger<FeishuProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        FeishuConfig config,
        string message,
        FeishuSendOptions options)
    {
        var text = options.ContentFormat == MessageContentFormat.Markdown
            ? RegexPatterns.StripMarkdown(message)
            : message;

        var payload = new
        {
            msg_type = "text",
            content = new
            {
                text = text
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        using var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
