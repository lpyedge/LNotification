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

    [Fact]
    public void Bind_MissingSection_ReturnsDefaults()
    {
        // IConfiguration with no "LNotification" section at all
        var config = TestConfigurationSection.FromDictionary(
            new Dictionary<string, string?>
            {
                ["SomeOtherSection:Key"] = "value"
            });

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(1000, options.RetryDelayMs);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Empty(options.Providers);
    }

    [Fact]
    public void Bind_NumericProviderKeyWithoutProviderField_IsSkipped()
    {
        // Provider section with numeric key and no Provider/Type/ProviderName field
        // providerKey falls back to section.Key ("0") which is numeric → skipped
        var settings = new Dictionary<string, string?>
        {
            ["LNotification:Providers:0:Alias"] = "ops",
            ["LNotification:Providers:0:WebhookUrl"] = "https://example.com/webhook"
        };

        var config = TestConfigurationSection.FromDictionary(settings);

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Empty(options.Providers);
    }

    [Fact]
    public void Bind_UnknownProvider_IsSkipped()
    {
        var settings = new Dictionary<string, string?>
        {
            ["LNotification:Providers:0:Provider"] = "NonExistentProvider",
            ["LNotification:Providers:0:Alias"] = "test"
        };

        var config = TestConfigurationSection.FromDictionary(settings);

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Empty(options.Providers);
    }

    [Fact]
    public void Bind_MultipleProviders_AllCreated()
    {
        var settings = new Dictionary<string, string?>
        {
            ["LNotification:Providers:0:Provider"] = "Slack",
            ["LNotification:Providers:0:Alias"] = "dev",
            ["LNotification:Providers:0:WebhookUrl"] = "https://slack.example.com",
            ["LNotification:Providers:1:Provider"] = "Discord",
            ["LNotification:Providers:1:Alias"] = "ops",
            ["LNotification:Providers:1:WebhookUrl"] = "https://discord.example.com"
        };

        var config = TestConfigurationSection.FromDictionary(settings);

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Equal(2, options.Providers.Count);
        Assert.IsType<SlackProvider.SlackConfig>(options.Providers[0]);
        Assert.IsType<DiscordProvider.DiscordConfig>(options.Providers[1]);
    }

    [Fact]
    public void Bind_ProviderWithNoAlias_DefaultsToDefault()
    {
        var settings = new Dictionary<string, string?>
        {
            ["LNotification:Providers:0:Provider"] = "Slack",
            ["LNotification:Providers:0:WebhookUrl"] = "https://example.com/webhook"
        };

        var config = TestConfigurationSection.FromDictionary(settings);

        var options = NotificationOptionsBinder.Bind(config);

        Assert.Single(options.Providers);
        Assert.Equal("default", options.Providers[0].Alias);
    }
}
