using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using LNotification.Internal;
using LNotification.Providers;

namespace LNotification;

public sealed class NotificationService : IDisposable
{

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationConfiguration _configuration;
    private readonly object _optionsLock = new();
    private NotificationOptions _options;
    private ConcurrentDictionary<(Type, string), NotificationProviderBase> _providerCache = new();
    private readonly IDisposable _reloadSubscription;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        NotificationConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<NotificationService>();
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _options = NotificationOptionsBinder.Bind(configuration.Configuration);
        ValidateOptionsOrThrow(_options);

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

    public Task<bool> SendAsync(
        string providerName,
        string message,
        string? alias = null)
    {
        var providerType = NotificationProviderBase.FindProviderType(providerName)
            ?? throw new ArgumentException($"Unknown provider: '{providerName}'.", nameof(providerName));

        var resolvedAlias = alias ?? "default";
        var getOrCreate = typeof(NotificationService)
            .GetMethod(nameof(GetOrCreateProvider), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(providerType);
        var provider = (NotificationProviderBase)getOrCreate.Invoke(this, new object[] { resolvedAlias })!;
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
            try
            {
                ValidateOptionsOrThrow(newOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LNotification configuration reload failed; keeping previous configuration.");
                return;
            }

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
            var timeoutSeconds = options.TimeoutSeconds <= 0 ? 30 : options.TimeoutSeconds;
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
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

    private void ValidateOptionsOrThrow(NotificationOptions options)
    {
        var errors = new List<string>();

        foreach (var provider in options.Providers.Where(p => p.Enabled))
        {
            if (string.IsNullOrWhiteSpace(provider.Alias))
            {
                errors.Add($"{provider.GetType().Name}: Alias is required.");
            }

            switch (provider)
            {
                case SlackProvider.SlackConfig slack when string.IsNullOrWhiteSpace(slack.WebhookUrl):
                    errors.Add("SlackConfig: WebhookUrl is required.");
                    break;
                case TelegramProvider.TelegramConfig tg when string.IsNullOrWhiteSpace(tg.BotToken) || string.IsNullOrWhiteSpace(tg.ChatId):
                    errors.Add("TelegramConfig: BotToken and ChatId are required.");
                    break;
                case DiscordProvider.DiscordConfig dc when string.IsNullOrWhiteSpace(dc.WebhookUrl):
                    errors.Add("DiscordConfig: WebhookUrl is required.");
                    break;
                case TeamsProvider.TeamsConfig teams when string.IsNullOrWhiteSpace(teams.WebhookUrl):
                    errors.Add("TeamsConfig: WebhookUrl is required.");
                    break;
                case FeishuProvider.FeishuConfig feishu when string.IsNullOrWhiteSpace(feishu.WebhookUrl):
                    errors.Add("FeishuConfig: WebhookUrl is required.");
                    break;
                case WebhookProvider.WebhookConfig hook when string.IsNullOrWhiteSpace(hook.Url):
                    errors.Add("WebhookConfig: Url is required.");
                    break;
                case LineProvider.LineConfig line when string.IsNullOrWhiteSpace(line.ChannelAccessToken) || string.IsNullOrWhiteSpace(line.UserId):
                    errors.Add("LineConfig: ChannelAccessToken and UserId are required.");
                    break;
                case MattermostProvider.MattermostConfig mm when string.IsNullOrWhiteSpace(mm.WebhookUrl):
                    errors.Add("MattermostConfig: WebhookUrl is required.");
                    break;
                case GoogleChatProvider.GoogleChatConfig chat when string.IsNullOrWhiteSpace(chat.WebhookUrl):
                    errors.Add("GoogleChatConfig: WebhookUrl is required.");
                    break;
                case NtfyProvider.NtfyConfig ntfy when string.IsNullOrWhiteSpace(ntfy.ServerUrl) || string.IsNullOrWhiteSpace(ntfy.Topic):
                    errors.Add("NtfyConfig: ServerUrl and Topic are required.");
                    break;
                case GotifyProvider.GotifyConfig gotify when string.IsNullOrWhiteSpace(gotify.ServerUrl) || string.IsNullOrWhiteSpace(gotify.Token):
                    errors.Add("GotifyConfig: ServerUrl and Token are required.");
                    break;
                case PushoverProvider.PushoverConfig po when string.IsNullOrWhiteSpace(po.ApplicationToken) || string.IsNullOrWhiteSpace(po.UserKey):
                    errors.Add("PushoverConfig: ApplicationToken and UserKey are required.");
                    break;
                case EmailProvider.EmailConfig email when string.IsNullOrWhiteSpace(email.SmtpHost) ||
                                                        string.IsNullOrWhiteSpace(email.FromAddress) ||
                                                        email.To == null || email.To.Count == 0:
                    errors.Add("EmailConfig: SmtpHost, FromAddress, and at least one To address are required.");
                    break;
                case MsGraphEmailProvider.MsGraphEmailConfig graph when
                    string.IsNullOrWhiteSpace(graph.TenantId) ||
                    string.IsNullOrWhiteSpace(graph.ClientId) ||
                    string.IsNullOrWhiteSpace(graph.ClientSecret) ||
                    string.IsNullOrWhiteSpace(graph.FromAddress) ||
                    graph.To == null || graph.To.Count == 0:
                    errors.Add("MsGraphEmailConfig: TenantId, ClientId, ClientSecret, FromAddress, and at least one To address are required.");
                    break;
                case FeishuProvider.FeishuConfig:
                case EmailProvider.EmailConfig:
                case MsGraphEmailProvider.MsGraphEmailConfig:
                case LineProvider.LineConfig:
                case WebhookProvider.WebhookConfig:
                case SlackProvider.SlackConfig:
                case TelegramProvider.TelegramConfig:
                case DiscordProvider.DiscordConfig:
                case TeamsProvider.TeamsConfig:
                case GotifyProvider.GotifyConfig:
                case NtfyProvider.NtfyConfig:
                case PushoverProvider.PushoverConfig:
                case MattermostProvider.MattermostConfig:
                case GoogleChatProvider.GoogleChatConfig:
                    // Other optional fields are allowed to be empty.
                    break;
                default:
                    // Unknown or custom provider configs are ignored here.
                    break;
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Invalid LNotification configuration: {string.Join(" ", errors)}");
        }
    }
}
