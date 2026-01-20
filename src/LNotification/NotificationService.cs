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

public sealed class NotificationService
{
    public enum NotifyLevel
    {
        Success,
        Info,
        Warning,
        Error,
        Critical
    }

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly NotificationConfiguration _configuration;
    private readonly object _optionsLock = new();
    private NotificationOptions _options;
    private IChangeToken _reloadToken;
    private readonly ConcurrentDictionary<(Type, string), NotificationProviderBase> _providerCache = new();

    internal NotificationService(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        NotificationConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = NotificationOptionsBinder.Bind(configuration.Configuration);
        _reloadToken = configuration.Configuration.GetReloadToken();
    }

    public Task<bool> SendAsync<TProvider>(
        string message,
        NotifyLevel level = NotifyLevel.Info,
        string? alias = null)
        where TProvider : NotificationProviderBase
    {
        var provider = GetOrCreateProvider<TProvider>(alias ?? "default");
        return provider.SendAsync(message, level, alias);
    }

    public Task<bool> SendMarkdownAsync<TProvider>(
        string markdownContent,
        NotifyLevel level = NotifyLevel.Info,
        string? alias = null)
        where TProvider : NotificationProviderBase
    {
        var provider = GetOrCreateProvider<TProvider>(alias ?? "default");
        return provider.SendMarkdownAsync(markdownContent, level, alias);
    }

    private TProvider GetOrCreateProvider<TProvider>(string alias)
        where TProvider : NotificationProviderBase
    {
        var options = GetOptions();
        var key = (typeof(TProvider), alias);
        
        if (_providerCache.TryGetValue(key, out var cachedProvider) && cachedProvider is TProvider typedProvider)
        {
            return typedProvider;
        }

        var logger = _loggerFactory.CreateLogger<TProvider>();
        var newProvider = (TProvider)Activator.CreateInstance(
            typeof(TProvider),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { _httpClientFactory, logger, options },
            culture: null)!;

        _providerCache[key] = newProvider;
        return newProvider;
    }

    private NotificationOptions GetOptions()
    {
        var currentToken = _reloadToken;
        if (!currentToken.HasChanged)
        {
            return _options;
        }

        lock (_optionsLock)
        {
            // 重新检查,因为其他线程可能已经更新
            if (_reloadToken == currentToken && currentToken.HasChanged)
            {
                var newOptions = NotificationOptionsBinder.Bind(_configuration.Configuration);
                var newToken = _configuration.Configuration.GetReloadToken();
                
                // 先清空缓存,再更新配置
                _providerCache.Clear();
                _options = newOptions;
                _reloadToken = newToken;
            }
            
            return _options;
        }
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
