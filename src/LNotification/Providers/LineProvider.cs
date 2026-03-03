using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class LineProvider : NotificationProviderBase
{
    private const string LinePushApiUrl = "https://api.line.me/v2/bot/message/push";

    public sealed class LineConfig : ProviderConfigBase
    {
        public string ChannelAccessToken { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    internal LineProvider(
        IHttpClientFactory factory,
        ILogger<LineProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (LineConfig)config;
        var payload = new
        {
            to = c.UserId,
            messages = new[]
            {
                new
                {
                    type = "text",
                    text = $"{Emoji(level)} {message}"
                }
            }
        };

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var request = new HttpRequestMessage(HttpMethod.Post, LinePushApiUrl)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {c.ChannelAccessToken}");

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
