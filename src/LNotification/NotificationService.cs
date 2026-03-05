using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using LNotification.Internal;

namespace LNotification;

public sealed class NotificationService : IDisposable
{

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly NotificationConfiguration _configuration;
    private readonly object _optionsLock = new();
    private NotificationOptions _options;
    private ConcurrentDictionary<(Type, string), NotificationProviderBase> _providerCache = new();
    private readonly IDisposable _reloadSubscription;

    internal NotificationService(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        NotificationConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = NotificationOptionsBinder.Bind(configuration.Configuration);

        _reloadSubscription = ChangeToken.OnChange(
            () => _configuration.Configuration.GetReloadToken(),
            ReloadOptions);
    }

    public Task<bool> SendAsync<TProvider>(
        string message,
        string? alias = null)
        where TProvider : NotificationProviderBase
    {
        var resolvedAlias = alias ?? "default";
        var provider = GetOrCreateProvider<TProvider>(resolvedAlias);
        return provider.SendAsync(message, resolvedAlias);
    }

    public Task<bool> SendAsync<TProvider, TOptions>(
        string message,
        Action<TOptions> configure,
        string? alias = null)
        where TProvider : NotificationProviderBase, INotificationProvider<TOptions>
        where TOptions : SendOptions, new()
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var resolvedAlias = alias ?? "default";
        var provider = GetOrCreateProvider<TProvider>(resolvedAlias);
        return provider.SendAsync(message, resolvedAlias, configure);
    }

    private TProvider GetOrCreateProvider<TProvider>(string alias)
        where TProvider : NotificationProviderBase
    {
        var key = (typeof(TProvider), alias);
        var cache = System.Threading.Volatile.Read(ref _providerCache);

        if (cache.TryGetValue(key, out var cachedProvider) && cachedProvider is TProvider typedProvider)
        {
            return typedProvider;
        }

        var options = GetOptions();
        var logger = _loggerFactory.CreateLogger<TProvider>();
        var newProvider = (TProvider)Activator.CreateInstance(
            typeof(TProvider),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { _httpClientFactory, logger, options },
            culture: null)!;

        if (cache.TryAdd(key, newProvider))
        {
            return newProvider;
        }

        if (cache.TryGetValue(key, out var existingProvider) && existingProvider is TProvider existingTyped)
        {
            return existingTyped;
        }

        return newProvider;
    }

    private NotificationOptions GetOptions()
    {
        return System.Threading.Volatile.Read(ref _options);
    }

    private void ReloadOptions()
    {
        lock (_optionsLock)
        {
            var newOptions = NotificationOptionsBinder.Bind(_configuration.Configuration);
            System.Threading.Volatile.Write(ref _options, newOptions);
            System.Threading.Volatile.Write(ref _providerCache, new ConcurrentDictionary<(Type, string), NotificationProviderBase>());
        }
    }

    void IDisposable.Dispose()
    {
        _reloadSubscription.Dispose();
    }

    public static IServiceCollection AddLNotification(
        IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var notificationConfiguration = new NotificationConfiguration(configuration);

        services.AddSingleton(notificationConfiguration);
        services.AddHttpClient(NotificationProviderBase.NotificationHttpClient, (sp, client) =>
        {
            var options = NotificationOptionsBinder.Bind(
                sp.GetRequiredService<NotificationConfiguration>().Configuration);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<NotificationService>();

        return services;
    }

    public static IServiceCollection AddLNotification(
        IServiceCollection services,
        string jsonPath)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("JSON path is required.", nameof(jsonPath));

        var fullPath = Path.GetFullPath(jsonPath);
        var basePath = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.GetFileName(fullPath), optional: false, reloadOnChange: true)
            .Build();

        return AddLNotification(services, configuration);
    }
}
