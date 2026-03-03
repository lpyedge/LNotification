using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class TeamsProvider : NotificationProviderBase
{
    /// <summary>
    /// Per-message options for Microsoft Teams webhook notifications.
    /// </summary>
    public sealed class TeamsSendOptions : SendOptions
    {
        /// <summary>Custom card activity title. Overrides default "[emoji] [level]".</summary>
        public string? Title { get; set; }
    }

    public sealed class TeamsConfig : ProviderConfigBase
    {
        public string WebhookUrl { get; set; } = string.Empty;
    }

    internal TeamsProvider(
        IHttpClientFactory factory,
        ILogger<TeamsProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (TeamsConfig)config;
        var o = options as TeamsSendOptions;

        var payload = new
        {
            @type = "MessageCard",
            @context = "https://schema.org/extensions",
            summary = $"[{level}] Notification",
            themeColor = GetThemeColor(level),
            sections = new[]
            {
                new
                {
                    activityTitle = o?.Title ?? $"{Emoji(level)} {level}",
                    text = message
                }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(c.WebhookUrl, payload);
        await EnsureSuccessAsync(response, c.Alias);
    }

    protected override Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var plain = RegexPatterns.StripMarkdown(markdownContent);
        return SendInternalAsync(config, plain, level, options);
    }

    private static string GetThemeColor(NotificationService.NotifyLevel level) => level switch
    {
        NotificationService.NotifyLevel.Success => "28a745",
        NotificationService.NotifyLevel.Info => "17a2b8",
        NotificationService.NotifyLevel.Warning => "ffc107",
        NotificationService.NotifyLevel.Error => "dc3545",
        NotificationService.NotifyLevel.Critical => "721c24",
        _ => "6c757d"
    };
}
