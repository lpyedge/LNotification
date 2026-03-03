# MattermostProvider

## Config keys

- Provider: "Mattermost" (or "MattermostProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Mattermost incoming webhook URL (required)
- Channel: optional channel override (e.g. "#ops" or "@username")

## How to get WebhookUrl

1. Log in to your Mattermost instance.
2. Go to Main Menu → Integrations → Incoming Webhooks → Add Incoming Webhook.
3. Choose a channel and fill in the details.
4. Copy the generated webhook URL.

You need the "Manage Incoming Webhooks" permission.

## Example

```json
{
  "Provider": "Mattermost",
  "Alias": "infra",
  "WebhookUrl": "https://mattermost.example.com/hooks/xxxxxxxx",
  "Channel": "#alerts"
}
```

## Notes

- Mattermost natively supports Markdown in webhook messages.
- If Channel is omitted, the message goes to the channel configured in the webhook.

## SendOptions

Use `MattermostSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| Username | `string?` | null | Override bot display name |
| IconUrl | `string?` | null | Override bot avatar URL |
| Channel | `string?` | null | Override target channel |

## References

- https://developers.mattermost.com/integrate/webhooks/incoming/
