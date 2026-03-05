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

public sealed class TelegramProvider : NotificationProviderBase<TelegramProvider.TelegramConfig, TelegramProvider.TelegramSendOptions>
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

        /// <summary>Override parse mode for plain text content. Ignored when ContentFormat is Markdown.</summary>
        public TelegramParseMode ParseMode { get; set; } = TelegramParseMode.None;
    }

    public sealed class TelegramConfig : ProviderConfigBase, IProviderSendOptions<TelegramSendOptions>
    {
        public string BotToken { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public TelegramSendOptions SendOptions { get; set; } = new();
    }

    internal TelegramProvider(
        IHttpClientFactory factory,
        ILogger<TelegramProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        TelegramConfig config,
        string message,
        TelegramSendOptions options)
    {
        var url = $"https://api.telegram.org/bot{config.BotToken}/sendMessage";
        var payload = BuildPayload(config, message, options);

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        using var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static object BuildPayload(
        TelegramConfig config,
        string message,
        TelegramSendOptions options)
    {
        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            var safeMarkdown = RegexPatterns.EscapeTelegramMarkdown(message);
            return new
            {
                chat_id = config.ChatId,
                text = safeMarkdown,
                parse_mode = "MarkdownV2",
                message_thread_id = options.MessageThreadId,
                disable_notification = options.DisableNotification,
                protect_content = options.ProtectContent
            };
        }

        var parseModeStr = options.ParseMode switch
        {
            TelegramParseMode.MarkdownV2 => "MarkdownV2",
            TelegramParseMode.Html => "HTML",
            _ => (string?)null
        };

        return new
        {
            chat_id = config.ChatId,
            text = message,
            parse_mode = parseModeStr,
            message_thread_id = options.MessageThreadId,
            disable_notification = options.DisableNotification,
            protect_content = options.ProtectContent
        };
    }
}
