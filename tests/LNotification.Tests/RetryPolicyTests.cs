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

    [Fact]
    public async Task RetryAsync_SucceedsFirstTime_NoRetry()
    {
        var options = new NotificationOptions
        {
            MaxRetries = 3,
            RetryDelayMs = 0
        };

        var provider = new RetryTestProvider(options);
        var attempts = 0;

        await provider.ExecuteRetryAsync(() =>
        {
            attempts++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task RetryAsync_SucceedsOnSecondAttempt_StopsRetrying()
    {
        var options = new NotificationOptions
        {
            MaxRetries = 3,
            RetryDelayMs = 0
        };

        var provider = new RetryTestProvider(options);
        var attempts = 0;

        await provider.ExecuteRetryAsync(() =>
        {
            attempts++;
            if (attempts < 2)
            {
                return Task.FromException(new InvalidOperationException("transient"));
            }
            return Task.CompletedTask;
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task RetryAsync_ZeroMaxRetries_OnlyOneAttempt()
    {
        var options = new NotificationOptions
        {
            MaxRetries = 0,
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

        Assert.Equal(1, attempts);
    }
}
