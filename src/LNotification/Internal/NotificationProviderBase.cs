using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LNotification.Internal;

public abstract class NotificationProviderBase
{
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;

    private readonly IReadOnlyList<ProviderConfigBase> _configs;
    private readonly int _maxRetries;
    private readonly int _retryDelayMs;
    protected readonly int TimeoutSeconds;

    protected NotificationProviderBase(
        IHttpClientFactory factory,
        ILogger logger,
        NotificationOptions options)
    {
        HttpClientFactory = factory;
        Logger = logger;
        _maxRetries = options.MaxRetries;
        _retryDelayMs = options.RetryDelayMs;
        TimeoutSeconds = options.TimeoutSeconds;

        // 根据Provider类名查找配置（例如：SlackProvider → SlackConfig）
        var providerName = GetType().Name.Replace("Provider", "");
        _configs = options.Providers
            .Where(c => c.GetType().Name.StartsWith(providerName) && c.Enabled)
            .ToList();
    }

    public async Task<bool> SendAsync(
        string message,
        NotificationService.NotifyLevel level,
        string? alias)
    {
        var config = ResolveConfig(alias);
        if (config == null)
        {
            return false;
        }

        try
        {
            await RetryAsync(async () =>
            {
                await SendInternalAsync(config, message, level);
            });

            Logger.LogInformation("{Provider}({Alias}) sent successfully",
                GetType().Name, config.Alias);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider}({Alias}) failed after retries",
                GetType().Name, config.Alias);

            if (level == NotificationService.NotifyLevel.Critical)
            {
                throw;
            }

            return false;
        }
    }

    public async Task<bool> SendMarkdownAsync(
        string markdownContent,
        NotificationService.NotifyLevel level,
        string? alias)
    {
        var config = ResolveConfig(alias);
        if (config == null)
        {
            return false;
        }

        try
        {
            await RetryAsync(async () =>
            {
                await SendMarkdownInternalAsync(config, markdownContent, level);
            });

            Logger.LogInformation("{Provider}({Alias}) markdown sent successfully",
                GetType().Name, config.Alias);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider}({Alias}) markdown failed after retries",
                GetType().Name, config.Alias);

            if (level == NotificationService.NotifyLevel.Critical)
            {
                throw;
            }

            return false;
        }
    }

    protected async Task RetryAsync(Func<Task> action)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < _maxRetries)
                {
                    var delay = _retryDelayMs * (int)Math.Pow(2, attempt);

                    Logger.LogWarning(ex,
                        "Attempt {Attempt}/{MaxRetries} failed, retrying in {Delay}ms",
                        attempt + 1, _maxRetries, delay);

                    await Task.Delay(delay);
                }
            }
        }

        throw lastException!;
    }

    protected abstract Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level);

    protected virtual Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var plain = RegexPatterns.StripMarkdown(markdownContent);
        return SendInternalAsync(config, plain, level);
    }

    protected async Task EnsureSuccessAsync(HttpResponseMessage response, string? alias)
    {
        if (!response.IsSuccessStatusCode)
        {
            string? content = null;
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                content = null;
            }

            Logger.LogError(
                "{Provider}({Alias}) failed with status {StatusCode}. Response: {Content}",
                GetType().Name,
                string.IsNullOrWhiteSpace(alias) ? "default" : alias,
                response.StatusCode,
                string.IsNullOrWhiteSpace(content) ? "<empty>" : content);
        }

        response.EnsureSuccessStatusCode();
    }

    private ProviderConfigBase? ResolveConfig(string? alias)
    {
        if (_configs.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(alias))
        {
            var cfg = _configs.FirstOrDefault(c =>
                string.Equals(c.Alias, alias, StringComparison.OrdinalIgnoreCase));

            if (cfg == null)
            {
                Logger.LogWarning(
                    "{Provider} alias '{Alias}' not found",
                    GetType().Name,
                    alias);
            }

            return cfg;
        }

        return _configs[0];
    }

    protected static string Emoji(NotificationService.NotifyLevel level) => level switch
    {
        NotificationService.NotifyLevel.Success => "✅",
        NotificationService.NotifyLevel.Info => "ℹ️",
        NotificationService.NotifyLevel.Warning => "⚠️",
        NotificationService.NotifyLevel.Error => "❌",
        NotificationService.NotifyLevel.Critical => "🚨",
        _ => "📢"
    };
}
