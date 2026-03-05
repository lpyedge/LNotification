using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LNotification.Internal;
using LNotification.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class WebhookProviderTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        internal HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        internal TestHttpClientFactory(HttpMessageHandler handler)
        {
            _client = new HttpClient(handler, disposeHandler: false);
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private static WebhookProvider CreateProvider(
        CapturingHandler handler,
        WebhookProvider.WebhookConfig config)
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
            RetryDelayMs = 0
        };
        options.Providers.Add(config);

        var factory = new TestHttpClientFactory(handler);
        return new WebhookProvider(factory, NullLogger<WebhookProvider>.Instance, options);
    }

    [Fact]
    public async Task SendAsync_JsonTemplate_EscapesMessageAsJsonString()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, new WebhookProvider.WebhookConfig
        {
            Alias = "default",
            Url = "https://example.com/hook",
            BodyTemplate = "{\"text\":\"{message}\"}"
        });

        var result = await provider.SendAsync<WebhookProvider.WebhookSendOptions>(
            "line\"1\nline2",
            "default",
            o => o.ContentType = WebhookContentType.Json);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Equal("{\"text\":\"line\\u00221\\nline2\"}", body);
    }

    [Fact]
    public async Task SendAsync_XmlTemplate_EscapesMessageAsXml()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, new WebhookProvider.WebhookConfig
        {
            Alias = "default",
            Url = "https://example.com/hook",
            BodyTemplate = "<notification>{message}</notification>"
        });

        var result = await provider.SendAsync<WebhookProvider.WebhookSendOptions>(
            "<tag>&value",
            "default",
            o => o.ContentType = WebhookContentType.Xml);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Equal("<notification>&lt;tag&gt;&amp;value</notification>", body);
    }

    [Fact]
    public async Task SendAsync_DefaultJsonPayload_ContainsTextField()
    {
        var handler = new CapturingHandler();
        var provider = CreateProvider(handler, new WebhookProvider.WebhookConfig
        {
            Alias = "default",
            Url = "https://example.com/hook"
        });

        var result = await provider.SendAsync("hello", "default");

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.NotNull(handler.LastRequest!.Content);
        var body = await handler.LastRequest.Content.ReadAsStringAsync();
        Assert.Contains("\"text\":\"hello\"", body);
    }
}
