using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class LineProvider : NotificationProviderBase<LineProvider.LineConfig, LineProvider.LineSendOptions>
{
    private const string LinePushApiUrl = "https://api.line.me/v2/bot/message/push";

    public sealed class LineSendOptions : SendOptions
    {
    }

    public sealed class LineConfig : ProviderConfigBase, IProviderSendOptions<LineSendOptions>
    {
        public string ChannelAccessToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public LineSendOptions SendOptions { get; set; } = new();
    }

    internal LineProvider(
        IHttpClientFactory factory,
        ILogger<LineProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        LineConfig config,
        string message,
        LineSendOptions options)
    {
        var text = options.ContentFormat == MessageContentFormat.Markdown
            ? RegexPatterns.StripMarkdown(message)
            : message;
        var payload = new
        {
            to = config.UserId,
            messages = new[]
            {
                new
                {
                    type = "text",
                    text = text
                }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var request = new HttpRequestMessage(HttpMethod.Post, LinePushApiUrl)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {config.ChannelAccessToken}");

        using var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, config.Alias);
    }
}
