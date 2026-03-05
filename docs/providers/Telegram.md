# TelegramProvider

## Config keys

- Provider: "Telegram" (or "TelegramProvider")
- Alias: optional string, defaults to "default"
- BotToken: token from BotFather
- ChatId: target chat or channel ID

## How to get BotToken

1. Log in to Telegram.
2. Open the BotFather chat and create a new bot with /newbot.
3. BotFather returns a token (BotToken).

## How to get ChatId

1. Start a chat with your bot (or add it to a group/channel).
2. Send a message to the bot.
3. Call:
   https://api.telegram.org/bot<BotToken>/getUpdates
4. Find the chat ID in the response payload ("message.chat.id" or "channel_post.chat.id").

## Example

```json
{
  "Provider": "Telegram",
  "Alias": "alerts",
  "BotToken": "123456:ABCDEF...",
  "ChatId": "123456789"
}
```

## SendOptions

Use `TelegramSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` |
| MessageThreadId | `int?` | null | Forum topic ID for supergroups |
| DisableNotification | `bool` | false | Send silently (no sound) |
| ProtectContent | `bool` | false | Prevent forwarding/saving |
| ParseMode | `TelegramParseMode` | None | `None` / `MarkdownV2` / `Html` |

```csharp
await service.SendAsync<TelegramProvider, TelegramProvider.TelegramSendOptions>(
    "msg",
    o =>
    {
        o.MessageThreadId = 42;
        o.ContentFormat = MessageContentFormat.Markdown;
    });
```

## References

- https://core.telegram.org/bots#creating-a-new-bot
- https://core.telegram.org/bots/api#getupdates
- https://core.telegram.org/bots/api#sendmessage
