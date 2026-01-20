using System.Collections.Generic;

namespace LNotification.Internal;

public sealed class NotificationOptions
{
    public List<ProviderConfigBase> Providers { get; } = new();
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int TimeoutSeconds { get; set; } = 30;
}
