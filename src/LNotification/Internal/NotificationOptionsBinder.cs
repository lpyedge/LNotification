using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LNotification.Internal;

internal static class NotificationOptionsBinder
{
    private static readonly Type[] ConfigTypes = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ProviderConfigBase)))
        .ToArray();

    internal static NotificationOptions Bind(IConfiguration configuration)
    {
        var options = new NotificationOptions();
        var lNotificationSection = configuration.GetSection("LNotification");
        if (!lNotificationSection.Exists())
        {
            return options;
        }

        options.MaxRetries = lNotificationSection.GetValue<int?>("MaxRetries") ?? options.MaxRetries;
        options.RetryDelayMs = lNotificationSection.GetValue<int?>("RetryDelayMs") ?? options.RetryDelayMs;
        options.TimeoutSeconds = lNotificationSection.GetValue<int?>("TimeoutSeconds") ?? options.TimeoutSeconds;

        var providersSection = lNotificationSection.GetSection("Providers");
        foreach (var providerSection in providersSection.GetChildren())
        {
            var providerKey = providerSection.GetValue<string>("Provider")
                ?? providerSection.GetValue<string>("Type")
                ?? providerSection.GetValue<string>("ProviderName")
                ?? providerSection.Key;

            if (string.IsNullOrWhiteSpace(providerKey) || int.TryParse(providerKey, out _))
            {
                continue;
            }

            var configType = FindConfigType(providerKey);
            if (configType == null)
            {
                continue;
            }

            var config = (ProviderConfigBase)Activator.CreateInstance(configType)!;
            providerSection.Bind(config);

            if (string.IsNullOrWhiteSpace(config.Alias))
            {
                config.Alias = "default";
            }

            options.Providers.Add(config);
        }

        return options;
    }

    private static Type? FindConfigType(string providerKey)
    {
        // 规范化：移除"Provider"后缀
#if NETSTANDARD2_0
        var normalized = providerKey.Replace("Provider", "").Replace("Config", "");
        
        // 查找匹配的Config类型（例如：Slack → SlackConfig）
        return ConfigTypes.FirstOrDefault(t =>
            t.Name.Equals($"{normalized}Config", StringComparison.OrdinalIgnoreCase));
#else
        var normalized = providerKey.Replace("Provider", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Config", "", StringComparison.OrdinalIgnoreCase);

        // 查找匹配的Config类型（例如：Slack → SlackConfig）
        return ConfigTypes.FirstOrDefault(t =>
            t.Name.Equals($"{normalized}Config", StringComparison.OrdinalIgnoreCase));
#endif
    }
}
