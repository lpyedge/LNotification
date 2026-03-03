using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class PushoverProvider : NotificationProviderBase
{
    private const string PushoverApiUrl = "https://api.pushover.net/1/messages.json";

    public sealed class PushoverConfig : ProviderConfigBase
    {
        public string ApplicationToken { get; set; } = string.Empty;
        public string UserKey { get; set; } = string.Empty;
        public int Priority { get; set; } = 0;
        public string? Sound { get; set; }
    }

    internal PushoverProvider(
        IHttpClientFactory factory,
        ILogger<PushoverProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (PushoverConfig)config;
        var payload = new Dictionary<string, string>
        {
            ["token"] = c.ApplicationToken,
            ["user"] = c.UserKey,
            ["title"] = $"{Emoji(level)} [{level}]",
            ["message"] = message,
            ["priority"] = c.Priority.ToString()
        };

        if (!string.IsNullOrWhiteSpace(c.Sound))
        {
            payload["sound"] = c.Sound!;
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsync(
            PushoverApiUrl,
            new FormUrlEncodedContent(payload.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))));
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var c = (PushoverConfig)config;
        var payload = new Dictionary<string, string>
        {
            ["token"] = c.ApplicationToken,
            ["user"] = c.UserKey,
            ["title"] = $"{Emoji(level)} [{level}]",
            ["message"] = markdownContent,
            ["priority"] = c.Priority.ToString(),
            ["html"] = "1"
        };

        if (!string.IsNullOrWhiteSpace(c.Sound))
        {
            payload["sound"] = c.Sound!;
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var htmlBody = RegexPatterns.MarkdownToHtml(markdownContent);
        payload["message"] = htmlBody;
        var response = await client.PostAsync(
            PushoverApiUrl,
            new FormUrlEncodedContent(payload.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))));
        await EnsureSuccessAsync(response, c.Alias);
    }
}
