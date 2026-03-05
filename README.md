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

NotificationService.AddLNotification(builder.Services, builder.Configuration);
```

Or load from a dedicated JSON file (relative or absolute path, reloads on change):

```csharp
using LNotification;
using LNotification.Providers;

NotificationService.AddLNotification(builder.Services, "lnotification.json");
```

The JSON file should include a top-level `LNotification` section.

## Send notifications

```csharp
using LNotification;
using LNotification.Internal;

var notifier = app.Services.GetRequiredService<NotificationService>();

await notifier.SendAsync<SlackProvider>("Hello");
await notifier.SendAsync<DiscordProvider, DiscordProvider.DiscordSendOptions>(
    "**Build OK**",
    o =>
    {
        o.ContentFormat = MessageContentFormat.Markdown;
        o.Username = "Build Bot";
    });

// Teams example with custom summary and theme color
await notifier.SendAsync<TeamsProvider, TeamsProvider.TeamsSendOptions>(
  "Service started",
  o =>
  {
    o.ContentFormat = MessageContentFormat.PlainText;
    o.Summary = "Service Alert";
    o.ThemeColor = System.Drawing.Color.FromArgb(0x28, 0xa7, 0x45);
  });
```

Supported providers:

- [SlackProvider](docs/providers/Slack.md)
- [TelegramProvider](docs/providers/Telegram.md)
- [DiscordProvider](docs/providers/Discord.md)
- [TeamsProvider](docs/providers/Teams.md)
- [FeishuProvider](docs/providers/Feishu.md)
- [EmailProvider](docs/providers/Email.md)
- [WebhookProvider](docs/providers/Webhook.md)
- [NtfyProvider](docs/providers/Ntfy.md)
- [GotifyProvider](docs/providers/Gotify.md)
- [PushoverProvider](docs/providers/Pushover.md)
- [LineProvider](docs/providers/Line.md)
- [MattermostProvider](docs/providers/Mattermost.md)
- [GoogleChatProvider](docs/providers/GoogleChat.md)
- [MsGraphEmailProvider](docs/providers/MsGraphEmail.md)

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
`SendAsync`.

`SendOptions` are applied as: defaults -> config -> per-call. Per-call customization uses
`Action<TOptions>`; the patch runs on a cloned copy, so config/defaults are never mutated.
The effective default for `SendOptions.ContentFormat` is `PlainText`.

## License

`LNotification` is released under the [GNU General Public License v3.0](LICENSE).
