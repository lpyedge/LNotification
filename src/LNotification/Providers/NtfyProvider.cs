using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;
public sealed class NtfyProvider : NotificationProviderBase<NtfyProvider.NtfyConfig, NtfyProvider.NtfySendOptions>
{
    /// <summary>
    /// Per-message options for ntfy notifications.
    /// </summary>
    public sealed class NtfySendOptions : SendOptions
    {
        /// <summary>Title/summary for the notification. Defaults to "Notification".</summary>
        public string Title { get; set; } = "Notification";

        /// <summary>Override message priority for this notification (1-5, default 3).</summary>
        public int? Priority { get; set; } = 3;

        /// <summary>Comma-separated emoji tags (e.g. "warning,skull", "white_check_mark").
        /// See https://docs.ntfy.sh/emojis/ for the full list.</summary>
        public string? Tags { get; set; }

        /// <summary>URL to open when the notification is clicked.</summary>
        public string? Url { get; set; }
    }

    public sealed class NtfyConfig : ProviderConfigBase, IProviderSendOptions<NtfySendOptions>
    {
        public string ServerUrl { get; set; } = "https://ntfy.sh";
        public string Topic { get; set; } = string.Empty;
        public string? Token { get; set; }
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
        NtfySendOptions options)
        
    {
        var url = $"{config.ServerUrl.TrimEnd('/')}/{config.Topic}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(message, Encoding.UTF8, "text/plain")
        };

        var priority = ResolvePriority(options.Priority);
        request.Headers.TryAddWithoutValidation("Priority", priority.ToString());
        var titleHeader = string.IsNullOrWhiteSpace(options.Title) ? "Notification" : options.Title;
        request.Headers.TryAddWithoutValidation("Title", titleHeader);

        if (!string.IsNullOrWhiteSpace(options.Tags))
        {
            request.Headers.TryAddWithoutValidation("Tags", options.Tags);
        }

        if (!string.IsNullOrWhiteSpace(options.Url))
        {
            request.Headers.TryAddWithoutValidation("Click", options.Url);
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
        using var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static int ResolvePriority(int? p)
    {
        var val = p ?? 3;
        if (val < 1) return 1;
        if (val > 5) return 5;
        return val;
    }
}
