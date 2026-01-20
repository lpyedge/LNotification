# LNotification

Provider-based notification service with a minimal public surface. Consumers only use
`NotificationService`, provider marker types, and `appsettings.json`.

## Install

```bash
dotnet add package LNotification
```

## Register

```csharp
using LNotification;
using LNotification.Providers;

builder.Services.AddLNotification(builder.Configuration);
```

Or load from a dedicated JSON file (relative or absolute path, reloads on change):

```csharp
using LNotification;
using LNotification.Providers;

builder.Services.AddLNotification("lnotification.json");
```

The JSON file should include a top-level `LNotification` section.

## Send notifications

```csharp
using LNotification;

var notifier = app.Services.GetRequiredService<NotificationService>();

await notifier.SendAsync<SlackProvider>("Hello", NotificationService.NotifyLevel.Info);
await notifier.SendMarkdownAsync<DiscordProvider>("**Build OK**");
```

Supported providers:

- [SlackProvider](docs/providers/Slack.md)
- [TelegramProvider](docs/providers/Telegram.md)
- [DiscordProvider](docs/providers/Discord.md)
- [TeamsProvider](docs/providers/Teams.md)
- [FeishuProvider](docs/providers/Feishu.md)
- [EmailProvider](docs/providers/Email.md)

## Configuration

See `examples/appsettings.sample.json` for a full example. The configuration section is
`LNotification` and accepts `MaxRetries`, `RetryDelayMs`, `TimeoutSeconds`, and a `Providers` array.

Example snippet:

```json
{
  "LNotification": {
    "MaxRetries": 3,
    "RetryDelayMs": 1000,
    "TimeoutSeconds": 30,
    "Providers": [
      {
        "Provider": "Slack",
        "Alias": "default",
        "WebhookUrl": "https://example.com/webhook"
      }
    ]
  }
}
```

You can select a specific provider configuration by passing the `alias` argument to
`SendAsync` or `SendMarkdownAsync`.

## License

`LNotification` is released under the [GNU General Public License v3.0](LICENSE).
