using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Common HTTP content types for webhook requests.</summary>
public enum WebhookContentType
{
    /// <summary>application/json</summary>
    Json,
    /// <summary>application/xml</summary>
    Xml,
    /// <summary>application/x-www-form-urlencoded</summary>
    FormUrlEncoded,
    /// <summary>text/plain</summary>
    PlainText
}

public sealed class WebhookProvider : NotificationProviderBase
{
    /// <summary>
    /// Per-message options for generic webhook notifications.
    /// </summary>
    public sealed class WebhookSendOptions : SendOptions
    {
        /// <summary>Override Content-Type for this request. Default: Json (application/json).</summary>
        public WebhookContentType ContentType { get; set; } = WebhookContentType.Json;
    }

    public sealed class WebhookConfig : ProviderConfigBase
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Custom body template. Use {message} and {level} placeholders.
        /// If null, sends JSON {"text":"..."}.
        /// </summary>
        public string? BodyTemplate { get; set; }
    }

    internal WebhookProvider(
        IHttpClientFactory factory,
        ILogger<WebhookProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level,
        SendOptions? options = null)
    {
        var c = (WebhookConfig)config;
        var o = options as WebhookSendOptions;
        var client = HttpClientFactory.CreateClient(NotificationHttpClient);

        var contentType = ResolveContentType(o?.ContentType ?? WebhookContentType.Json);

        HttpRequestMessage request;

        if (!string.IsNullOrWhiteSpace(c.BodyTemplate))
        {
            var template = c.BodyTemplate!;
            var body = template
                .Replace("{message}", message)
                .Replace("{level}", level.ToString());

            request = new HttpRequestMessage(new HttpMethod(c.Method), c.Url)
            {
                Content = new StringContent(body, Encoding.UTF8, contentType)
            };
        }
        else
        {
            var payload = new { text = $"{Emoji(level)} {message}" };
            request = new HttpRequestMessage(new HttpMethod(c.Method), c.Url)
            {
                Content = JsonContent.Create(payload)
            };
        }

        foreach (var header in c.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, c.Alias);
    }

    private static string ResolveContentType(WebhookContentType ct) => ct switch
    {
        WebhookContentType.Json => "application/json",
        WebhookContentType.Xml => "application/xml",
        WebhookContentType.FormUrlEncoded => "application/x-www-form-urlencoded",
        WebhookContentType.PlainText => "text/plain",
        _ => "application/json"
    };
}
