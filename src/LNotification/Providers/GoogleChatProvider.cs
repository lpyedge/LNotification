using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Google Chat thread reply behavior.</summary>
public enum GoogleChatReplyOption
{
    /// <summary>If threadKey matches an existing thread, reply to it; otherwise create a new thread.</summary>
    FallbackToNewThread,
    /// <summary>Always create a new thread regardless of threadKey.</summary>
    ForceNewThread
}

public sealed class GoogleChatProvider : NotificationProviderBase<GoogleChatProvider.GoogleChatConfig, GoogleChatProvider.GoogleChatSendOptions>
{
    /// <summary>
    /// Per-message options for Google Chat webhook notifications.
    /// </summary>
    public sealed class GoogleChatSendOptions : SendOptions
    {
        /// <summary>Thread key. Messages with the same key are grouped into the same conversation thread.</summary>
        public string? ThreadKey { get; set; }

        /// <summary>Thread reply behavior when ThreadKey is specified.</summary>
        public GoogleChatReplyOption ReplyOption { get; set; } = GoogleChatReplyOption.FallbackToNewThread;
    }

    public sealed class GoogleChatConfig : ProviderConfigBase, IProviderSendOptions<GoogleChatSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public GoogleChatSendOptions SendOptions { get; set; } = new();
    }

    internal GoogleChatProvider(
        IHttpClientFactory factory,
        ILogger<GoogleChatProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        GoogleChatConfig config,
        string message,
        
        GoogleChatSendOptions options)
    {
        var payload = new
        {
            text = message
        };

        var url = BuildUrl(config.WebhookUrl, options);
        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        using var response = await client.PostAsJsonAsync(url, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static string BuildUrl(string webhookUrl, GoogleChatSendOptions? o)
    {
        if (o == null || string.IsNullOrWhiteSpace(o.ThreadKey))
        {
            return webhookUrl;
        }

        var separator = webhookUrl.Contains("?") ? "&" : "?";
        var replyOption = o.ReplyOption == GoogleChatReplyOption.ForceNewThread
            ? "REPLY_MESSAGE_OR_FAIL"
            : "REPLY_MESSAGE_FALLBACK_TO_NEW_THREAD";

        return $"{webhookUrl}{separator}threadKey={Uri.EscapeDataString(o.ThreadKey)}&messageReplyOption={replyOption}";
    }
}
