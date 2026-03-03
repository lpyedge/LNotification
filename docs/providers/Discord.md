# DiscordProvider

## Config keys

- Provider: "Discord" (or "DiscordProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Discord channel webhook URL

## How to get WebhookUrl

1. Log in to Discord and open the target server.
2. Open the channel settings for the channel that will receive messages.
3. Go to Integrations -> Webhooks -> New Webhook.
4. Copy the Webhook URL.

You need the "Manage Webhooks" permission on the server/channel.

## Example

```json
{
  "Provider": "Discord",
  "Alias": "devops",
  "WebhookUrl": "https://discord.com/api/webhooks/..."
}
```

## SendOptions

Use `DiscordSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` |
| Username | `string?` | null | Override bot display name |
| AvatarUrl | `string?` | null | Override bot avatar URL |
| Flags | `DiscordMessageFlag` | None | `SuppressEmbeds` / `SuppressNotifications` |

```csharp
await service.SendAsync<DiscordProvider, DiscordProvider.DiscordSendOptions>(
    "msg",
    new DiscordProvider.DiscordSendOptions
    {
        ContentFormat = MessageContentFormat.Markdown,
        Username = "Alert Bot",
        Flags = DiscordMessageFlag.SuppressNotifications
    });
```

## References

- https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks
- https://discord.com/developers/docs/resources/webhook
