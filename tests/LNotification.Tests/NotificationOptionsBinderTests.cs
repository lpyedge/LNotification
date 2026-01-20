using System.Collections.Generic;
using LNotification.Internal;
using LNotification.Providers;
using Xunit;

namespace LNotification.Tests;

public class NotificationOptionsBinderTests
{
    [Fact]
    public void Bind_CreatesProviderConfigs()
    {
        var settings = new Dictionary<string, string?>
        {
            ["LNotification:MaxRetries"] = "5",
            ["LNotification:RetryDelayMs"] = "2000",
            ["LNotification:TimeoutSeconds"] = "45",
            ["LNotification:Providers:0:Provider"] = "Slack",
            ["LNotification:Providers:0:Alias"] = "ops",
            ["LNotification:Providers:0:WebhookUrl"] = "https://example.com/webhook"
        };

        var config = TestConfigurationSection.FromDictionary(settings);

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Equal(5, options.MaxRetries);
        Assert.Equal(2000, options.RetryDelayMs);
        Assert.Equal(45, options.TimeoutSeconds);
        Assert.Single(options.Providers);

        var slackConfig = Assert.IsType<SlackProvider.SlackConfig>(options.Providers[0]);
        Assert.Equal("ops", slackConfig.Alias);
        Assert.Equal("https://example.com/webhook", slackConfig.WebhookUrl);
    }
}
