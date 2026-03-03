using System;
using System.Linq;
using LNotification;
using LNotification.Internal;
using Xunit;

namespace LNotification.Tests;

public class SendOptionsDefaultsTests
{
    [Fact]
    public void ProviderConfigs_DefaultSendOptions_ContentFormatIsPlainText()
    {
        var providerConfigTypes = typeof(NotificationService).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(ProviderConfigBase)))
            .ToArray();

        Assert.NotEmpty(providerConfigTypes);

        foreach (var type in providerConfigTypes)
        {
            var sendOptionsProperty = type.GetProperty("SendOptions");
            if (sendOptionsProperty == null)
            {
                continue;
            }

            var instance = Activator.CreateInstance(type);
            Assert.NotNull(instance);

            var sendOptionsValue = sendOptionsProperty.GetValue(instance!);
            Assert.NotNull(sendOptionsValue);

            var options = Assert.IsAssignableFrom<SendOptions>(sendOptionsValue);
            Assert.Equal(
                MessageContentFormat.PlainText,
                options.ContentFormat);
        }
    }
}
