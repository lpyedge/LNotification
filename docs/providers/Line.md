# LineProvider

## Config keys

- Provider: "Line" (or "LineProvider")
- Alias: optional string, defaults to "default"
- ChannelAccessToken: LINE Messaging API channel access token (required)
- UserId: target user ID, group ID, or room ID (required)

## How to get ChannelAccessToken

1. Go to LINE Developers Console: https://developers.line.biz/console/
2. Create a provider and a Messaging API channel.
3. In the channel settings, issue a long-lived Channel Access Token.

## How to get UserId

1. Set up a webhook URL for your channel.
2. When a user adds your bot as a friend or sends a message, the webhook payload contains the userId.
3. Alternatively, use the LINE Official Account Manager to find user/group IDs.

## Example

```json
{
  "Provider": "Line",
  "Alias": "alerts",
  "ChannelAccessToken": "xxxxxxxxxxxx...",
  "UserId": "U1234567890abcdef"
}
```

## SendOptions

Use `LineSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` (Markdown is stripped to plain text) |

## References

- https://developers.line.biz/en/docs/messaging-api/
- https://developers.line.biz/en/reference/messaging-api/#send-push-message
