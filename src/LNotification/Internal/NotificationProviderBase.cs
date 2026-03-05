using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LNotification.Internal;

public abstract class NotificationProviderBase
{
    internal const string NotificationHttpClient = "LNotificationHttpClient";

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

        var providerName = GetType().Name.Replace("Provider", "");
        _configs = options.Providers
            .Where(c => c.GetType().Name.StartsWith(providerName) && c.Enabled)
            .ToList();
    }

    internal abstract Type SupportedSendOptionsType { get; }

    public async Task<bool> SendAsync(
        string message,
        string? alias)
    {
        var config = ResolveConfig(alias);
        if (config == null)
        {
            return false;
        }

        var defaultOptions = GetDefaultOptions(config);
        return await SendCoreAsync(config, message, defaultOptions);
    }

    public async Task<bool> SendAsync<TOptions>(
        string message,
        string? alias,
        Action<TOptions> configure)
        where TOptions : SendOptions, new()
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (SupportedSendOptionsType != typeof(TOptions))
        {
            throw new ArgumentException(
                $"Provider {GetType().Name} expects options type {SupportedSendOptionsType.Name}, but received {typeof(TOptions).Name}.",
                nameof(configure));
        }

        var config = ResolveConfig(alias);
        if (config == null)
        {
            return false;
        }

        // Clone config SendOptions for this send so per-call changes never mutate shared config state.
        var baseOptions = (TOptions)GetDefaultOptions(config);
        var effectiveOptions = CloneOptions(baseOptions);
        configure(effectiveOptions);

        return await SendCoreAsync(config, message, effectiveOptions);
    }

    protected async Task RetryAsync(Func<Task> action)
    {
        Exception? lastException = null;

        var maxRetries = _maxRetries < 0 ? 0 : _maxRetries;
        var retryDelayMs = _retryDelayMs < 0 ? 0 : _retryDelayMs;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt < maxRetries)
                {
                    var shift = attempt > 30 ? 30 : attempt;
                    var multiplier = 1L << shift;
                    var delayLong = (long)retryDelayMs * multiplier;
                    if (delayLong > int.MaxValue) delayLong = int.MaxValue;

                    var delay = (int)delayLong;

                    Logger.LogWarning(ex,
                        "Attempt {Attempt}/{MaxRetries} failed, retrying in {Delay}ms",
                        attempt + 1, maxRetries, delay);

                    await Task.Delay(delay);
                }
            }
        }

        throw lastException!;
    }

    protected abstract SendOptions GetDefaultOptions(ProviderConfigBase config);

    protected abstract Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        SendOptions options);

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

    private async Task<bool> SendCoreAsync(
        ProviderConfigBase config,
        string message,
        SendOptions options)
    {
        try
        {
            await RetryAsync(async () =>
            {
                await SendInternalAsync(config, message, options);
            });

            Logger.LogInformation("{Provider}({Alias}) sent successfully",
                GetType().Name, config.Alias);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider}({Alias}) failed after retries",
                GetType().Name, config.Alias);


            return false;
        }
    }

    private static TOptions CloneOptions<TOptions>(TOptions source)
        where TOptions : SendOptions, new()
    {
        // Shallow clone is sufficient for current option shapes (primitives/strings/enums/structs).
        var clone = new TOptions();

        foreach (var prop in typeof(TOptions).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

            if (prop.GetIndexParameters().Length != 0)
            {
                continue;
            }

            prop.SetValue(clone, prop.GetValue(source));
        }

        return clone;
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

}

public abstract class NotificationProviderBase<TConfig, TOptions> :
    NotificationProviderBase,
    INotificationProvider<TOptions>
    where TConfig : ProviderConfigBase, IProviderSendOptions<TOptions>
    where TOptions : SendOptions, new()
{
    protected NotificationProviderBase(
        IHttpClientFactory factory,
        ILogger logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    internal sealed override Type SupportedSendOptionsType => typeof(TOptions);

    protected sealed override SendOptions GetDefaultOptions(ProviderConfigBase config)
    {
        var typedConfig = (TConfig)config;
        typedConfig.SendOptions ??= new TOptions();
        return typedConfig.SendOptions;
    }

    protected sealed override Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        SendOptions options)
    {
        return SendInternalAsync((TConfig)config, message, (TOptions)options);
    }

    protected abstract Task SendInternalAsync(
        TConfig config,
        string message,
        TOptions options);

}
