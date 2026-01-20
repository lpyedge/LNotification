using System;
using Microsoft.Extensions.Configuration;

namespace LNotification.Internal;

internal sealed class NotificationConfiguration
{
    internal NotificationConfiguration(IConfiguration configuration)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    internal IConfiguration Configuration { get; }
}
