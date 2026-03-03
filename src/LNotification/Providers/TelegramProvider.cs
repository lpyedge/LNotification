using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Telegram message parse mode.</summary>
public enum TelegramParseMode
{
    /// <summary>No special formatting, plain text.</summary>
    None,
    /// <summary>MarkdownV2 formatting (Telegram-specific escape rules apply).</summary>
    MarkdownV2,
    /// <summary>HTML formatting (&lt;b&gt;, &lt;i&gt;, &lt;a&gt;, &lt;code&gt;, etc.).</summary>
    Html
}

public sealed class TelegramProvider : NotificationProviderBase
{
    /// <summary>
    /// Per-message options for Telegram notifications.
    /// </summary>
    public sealed class TelegramSendOptions : SendOptions
    {
        /// <summary>Forum topic ID. Sends message to a specific topic in a supergroup with forums enabled.</summary>
        public int? MessageThreadId { get; set; }

        /// <summary>Send the message silently. Users will receive a notification with no sound.</summary>
        public bool DisableNotification { get; set; }

        /// <summary>Protect the message from forwarding and saving by recipients.</summary>
        public bool ProtectContent { get; set; }

        /// <summary>Override parse mode for SendAsync (plain text calls). Default: None.
        /// For SendMarkdownAsync, MarkdownV2 is always used regardless of this setting.</summary>
        public TelegramParseMode ParseMode { get; set; } = TelegramParseMode.None;
    }

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
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (TelegramConfig)config;
        var o = options as TelegramSendOptions;
        var url = $"https://api.telegram.org/bot{c.BotToken}/sendMessage";

        var payload = BuildPayload(c, $"{Emoji(level)} {message}", o);

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override async Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (TelegramConfig)config;
        var o = options as TelegramSendOptions;
        var safeMarkdown = RegexPatterns.EscapeTelegramMarkdown(markdownContent);
        var url = $"https://api.telegram.org/bot{c.BotToken}/sendMessage";

        // SendMarkdownAsync always uses MarkdownV2
        var payload = new
        {
            chat_id = c.ChatId,
            text = $"{Emoji(level)} {safeMarkdown}",
            parse_mode = "MarkdownV2",
            message_thread_id = o?.MessageThreadId,
            disable_notification = o?.DisableNotification ?? false,
            protect_content = o?.ProtectContent ?? false
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    private static object BuildPayload(TelegramConfig c, string text, TelegramSendOptions? o)
    {
        var parseMode = o?.ParseMode ?? TelegramParseMode.None;
        var parseModeStr = parseMode switch
        {
            TelegramParseMode.MarkdownV2 => "MarkdownV2",
            TelegramParseMode.Html => "HTML",
            _ => (string?)null
        };

        return new
        {
            chat_id = c.ChatId,
            text = text,
            parse_mode = parseModeStr,
            message_thread_id = o?.MessageThreadId,
            disable_notification = o?.DisableNotification ?? false,
            protect_content = o?.ProtectContent ?? false
        };
    }
}
