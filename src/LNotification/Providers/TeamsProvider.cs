using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class TeamsProvider : NotificationProviderBase<TeamsProvider.TeamsConfig, TeamsProvider.TeamsSendOptions>
{
    /// <summary>
    /// Per-message options for Microsoft Teams webhook notifications.
    /// </summary>
    public sealed class TeamsSendOptions : SendOptions
    {
        /// <summary>Custom card activity title. Overrides default "[emoji] [level]".</summary>
        public string? Title { get; set; }
    }

    public sealed class TeamsConfig : ProviderConfigBase, IProviderSendOptions<TeamsSendOptions>
    {
        public string WebhookUrl { get; set; } = string.Empty;
        public TeamsSendOptions SendOptions { get; set; } = new();
    }

    internal TeamsProvider(
        IHttpClientFactory factory,
        ILogger<TeamsProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        TeamsConfig config,
        string message,
        NotificationService.NotifyLevel level,
        TeamsSendOptions options)
    {
        var text = options.ContentFormat == MessageContentFormat.Markdown
            ? RegexPatterns.StripMarkdown(message)
            : message;

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
                    activityTitle = options.Title ?? $"{Emoji(level)} {level}",
                    text = text
                }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
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
