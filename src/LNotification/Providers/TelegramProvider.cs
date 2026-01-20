using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class TelegramProvider : NotificationProviderBase
{
    public sealed class TelegramConfig : ProviderConfigBase
    {
        public string BotToken { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
    }

    internal TelegramProvider(
        IHttpClientFactory factory,
        ILogger<TelegramProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (TelegramConfig)config;
        var url = $"https://api.telegram.org/bot{c.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = c.ChatId,
            text = $"{Emoji(level)} {message}"
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient.Name);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var c = (TelegramConfig)config;
        var safeMarkdown = RegexPatterns.EscapeTelegramMarkdown(markdownContent);
        var url = $"https://api.telegram.org/bot{c.BotToken}/sendMessage";
        var payload = new
        {
            chat_id = c.ChatId,
            text = $"{Emoji(level)} {safeMarkdown}",
            parse_mode = "MarkdownV2"
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient.Name);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
