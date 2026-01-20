using System;
using System.Net.Http;
using System.Threading.Tasks;
using LNotification.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LNotification.Tests;

public class RetryPolicyTests
{
    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class RetryTestProvider : NotificationProviderBase
    {
        internal sealed class RetryConfig : ProviderConfigBase { }

        internal RetryTestProvider(NotificationOptions options)
            : base(new TestHttpClientFactory(), NullLogger.Instance, options) { }

        protected override Task SendInternalAsync(
            ProviderConfigBase config,
            string message,
            NotificationService.NotifyLevel level)
        {
            return Task.CompletedTask;
        }

        internal Task ExecuteRetryAsync(Func<Task> action)
        {
            return RetryAsync(action);
        }
    }

    [Fact]
    public async Task RetryAsync_StopsAfterMaxRetries()
    {
        var options = new NotificationOptions
        {
            MaxRetries = 2,
            RetryDelayMs = 0
        };

        var provider = new RetryTestProvider(options);
        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.ExecuteRetryAsync(() =>
            {
                attempts++;
                return Task.FromException(new InvalidOperationException("fail"));
            }));

        Assert.Equal(options.MaxRetries + 1, attempts);
    }
}
