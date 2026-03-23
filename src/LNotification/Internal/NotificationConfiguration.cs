using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace LNotification.Internal;

public sealed class NotificationConfiguration
{
    internal NotificationConfiguration(IConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    internal IConfiguration Configuration { get; }
}

public sealed class NotificationOptions
{
    public List<ProviderConfigBase> Providers { get; } = new();
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
}

internal static class NotificationOptionsBinder
{
    private const int DefaultTimeoutSeconds = 30;
    private const int MaxTimeoutSeconds = int.MaxValue / 1000;

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

        options.MaxRetries = NormalizeNonNegative(
            lNotificationSection.GetValue<int?>("MaxRetries"),
            options.MaxRetries);
        options.RetryDelayMs = NormalizeNonNegative(
            lNotificationSection.GetValue<int?>("RetryDelayMs"),
            options.RetryDelayMs);
        options.TimeoutSeconds = NormalizeTimeoutSeconds(
            lNotificationSection.GetValue<int?>("TimeoutSeconds"),
            options.TimeoutSeconds);

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
        var normalized = TrimKnownSuffix(providerKey, "Provider");
        normalized = TrimKnownSuffix(normalized, "Config");

        return ConfigTypes.FirstOrDefault(t =>
            t.Name.Equals($"{normalized}Config", StringComparison.OrdinalIgnoreCase));
    }

    private static int NormalizeNonNegative(int? value, int fallback)
    {
        if (!value.HasValue)
        {
            return fallback;
        }

        return value.Value < 0 ? 0 : value.Value;
    }

    private static int NormalizeTimeoutSeconds(int? value, int fallback)
    {
        if (!value.HasValue)
        {
            return fallback;
        }

        if (value.Value <= 0)
        {
            return DefaultTimeoutSeconds;
        }

        if (value.Value > MaxTimeoutSeconds)
        {
            return MaxTimeoutSeconds;
        }

        return value.Value;
    }

    private static string TrimKnownSuffix(string value, string suffix)
    {
        if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return value.Substring(0, value.Length - suffix.Length);
        }

        return value;
    }
}
