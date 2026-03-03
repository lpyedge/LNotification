using System.Collections.Generic;
using System.Linq;
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

public sealed class WebhookProvider : NotificationProviderBase<WebhookProvider.WebhookConfig, WebhookProvider.WebhookSendOptions>
{
    /// <summary>
    /// Per-message options for generic webhook notifications.
    /// </summary>
    public sealed class WebhookSendOptions : SendOptions
    {
        /// <summary>Override Content-Type for this request. Default: Json (application/json).</summary>
        public WebhookContentType ContentType { get; set; } = WebhookContentType.Json;
    }

    public sealed class WebhookConfig : ProviderConfigBase, IProviderSendOptions<WebhookSendOptions>
    {
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// Custom body template. Use {message} and {level} placeholders.
        /// If null, sends JSON {"text":"..."}.
        /// </summary>
        public string? BodyTemplate { get; set; }

        public WebhookSendOptions SendOptions { get; set; } = new();
    }

    internal WebhookProvider(
        IHttpClientFactory factory,
        ILogger<WebhookProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        WebhookConfig config,
        string message,
        NotificationService.NotifyLevel level,
        WebhookSendOptions options)
    {
        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var contentType = ResolveContentType(options.ContentType);
        var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url)
        {
            Content = BuildContent(config, message, level, contentType, options.ContentType)
        };

        foreach (var header in config.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static HttpContent BuildContent(
        WebhookConfig config,
        string message,
        NotificationService.NotifyLevel level,
        string contentType,
        WebhookContentType optionType)
    {
        if (!string.IsNullOrWhiteSpace(config.BodyTemplate))
        {
            var body = config.BodyTemplate!
                .Replace("{message}", message)
                .Replace("{level}", level.ToString());

            return new StringContent(body, Encoding.UTF8, contentType);
        }

        var text = $"{Emoji(level)} {message}";

        return optionType switch
        {
            WebhookContentType.Json => JsonContent.Create(new { text, level = level.ToString() }),
            WebhookContentType.FormUrlEncoded => new FormUrlEncodedContent(
                new Dictionary<string, string?>
                {
                    ["text"] = text,
                    ["level"] = level.ToString()
                }.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))),
            WebhookContentType.Xml => new StringContent(
                $"<notification><level>{level}</level><text>{text}</text></notification>",
                Encoding.UTF8,
                contentType),
            _ => new StringContent(text, Encoding.UTF8, contentType)
        };
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
