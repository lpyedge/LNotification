using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class NtfyProvider : NotificationProviderBase
{
    public sealed class NtfyConfig : ProviderConfigBase
    {
        public string ServerUrl { get; set; } = "https://ntfy.sh";
        public string Topic { get; set; } = string.Empty;
        public string? Token { get; set; }
        public int Priority { get; set; } = 3;
    }

    internal NtfyProvider(
        IHttpClientFactory factory,
        ILogger<NtfyProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (NtfyConfig)config;
        var url = $"{c.ServerUrl.TrimEnd('/')}/{c.Topic}";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent($"{Emoji(level)} {message}", Encoding.UTF8, "text/plain")
        };

        request.Headers.TryAddWithoutValidation("Priority", c.Priority.ToString());
        request.Headers.TryAddWithoutValidation("Title", $"[{level}] Notification");

        if (!string.IsNullOrWhiteSpace(c.Token))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {c.Token}");
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, c.Alias);
    }
}
