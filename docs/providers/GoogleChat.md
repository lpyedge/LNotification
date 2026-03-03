# GoogleChatProvider

## Config keys

- Provider: "GoogleChat" (or "GoogleChatProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Google Chat space webhook URL (required)

## How to get WebhookUrl

1. Open Google Chat and go to the target space.
2. Click the space name → Apps & Integrations → Manage webhooks.
3. Create a new webhook → copy the URL.

You need space manager permissions to create webhooks.

## Example

```json
{
  "Provider": "GoogleChat",
  "Alias": "team",
  "WebhookUrl": "https://chat.googleapis.com/v1/spaces/SPACE_ID/messages?key=KEY&token=TOKEN"
}
```

## SendOptions

Use `GoogleChatSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ThreadKey | `string?` | null | Thread key for grouping messages |
| ReplyOption | `GoogleChatReplyOption` | FallbackToNewThread | `FallbackToNewThread` / `ForceNewThread` |

## References

- https://developers.google.com/workspace/chat/quickstart/webhooks
