using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Drawing;
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
        public string Title { get; set; } = "Notification";
        /// <summary>Card summary text. Defaults to "Notification".</summary>
        public string Summary { get; set; } = "Notification";

        /// <summary>Theme color for the card as a System.Drawing.Color. Defaults to #6c757d.</summary>
        public Color ThemeColor { get; set; } = Color.FromArgb(0x6c, 0x75, 0x7d);
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
        TeamsSendOptions options)
    {
        var text = options.ContentFormat == MessageContentFormat.Markdown
            ? RegexPatterns.StripMarkdown(message)
            : message;

        string themeHex = $"{options.ThemeColor.R:X2}{options.ThemeColor.G:X2}{options.ThemeColor.B:X2}";
        var payload = new
        {
            @type = "MessageCard",
            @context = "https://schema.org/extensions",
            summary = options.Summary,
            themeColor = themeHex,
            sections = new[]
            {
                new
                {
                    activityTitle = string.IsNullOrWhiteSpace(options.Title) ? "Notification" : options.Title,
                    text = text
                }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationProviderBase.NotificationHttpClient);
        using var response = await client.PostAsJsonAsync(config.WebhookUrl, payload);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
