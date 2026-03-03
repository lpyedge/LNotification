using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Ntfy message priority level (1-5).</summary>
public enum NtfyPriority
{
    /// <summary>Min priority (1). No notification sound or vibration.</summary>
    Min = 1,
    /// <summary>Low priority (2). No notification sound.</summary>
    Low = 2,
    /// <summary>Default priority (3). Standard notification.</summary>
    Default = 3,
    /// <summary>High priority (4). Shows even with Do Not Disturb on some devices.</summary>
    High = 4,
    /// <summary>Urgent priority (5). Bypasses DND, shows persistent notification.</summary>
    Urgent = 5
}

public sealed class NtfyProvider : NotificationProviderBase<NtfyProvider.NtfyConfig, NtfyProvider.NtfySendOptions>
{
    /// <summary>
    /// Per-message options for ntfy notifications.
    /// </summary>
    public sealed class NtfySendOptions : SendOptions
    {
        /// <summary>Override message priority for this notification.</summary>
        public NtfyPriority? Priority { get; set; }

        /// <summary>Comma-separated emoji tags (e.g. "warning,skull", "white_check_mark").
        /// See https://docs.ntfy.sh/emojis/ for the full list.</summary>
        public string? Tags { get; set; }

        /// <summary>URL to open when the notification is clicked.</summary>
        public string? ClickUrl { get; set; }
    }

    public sealed class NtfyConfig : ProviderConfigBase, IProviderSendOptions<NtfySendOptions>
    {
        public string ServerUrl { get; set; } = "https://ntfy.sh";
        public string Topic { get; set; } = string.Empty;
        public string? Token { get; set; }
        public int Priority { get; set; } = 3;
        public NtfySendOptions SendOptions { get; set; } = new();
    }

    internal NtfyProvider(
        IHttpClientFactory factory,
        ILogger<NtfyProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        NtfyConfig config,
        string message,
        NotificationService.NotifyLevel level,
        NtfySendOptions options)
    {
        var url = $"{config.ServerUrl.TrimEnd('/')}/{config.Topic}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent($"{Emoji(level)} {message}", Encoding.UTF8, "text/plain")
        };

        var priority = options.Priority != null ? (int)options.Priority : config.Priority;
        request.Headers.TryAddWithoutValidation("Priority", priority.ToString());
        request.Headers.TryAddWithoutValidation("Title", $"[{level}] Notification");

        if (!string.IsNullOrWhiteSpace(options.Tags))
        {
            request.Headers.TryAddWithoutValidation("Tags", options.Tags);
        }

        if (!string.IsNullOrWhiteSpace(options.ClickUrl))
        {
            request.Headers.TryAddWithoutValidation("Click", options.ClickUrl);
        }

        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            request.Headers.TryAddWithoutValidation("Markdown", "yes");
        }

        if (!string.IsNullOrWhiteSpace(config.Token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.Token}");
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
