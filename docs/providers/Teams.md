# TeamsProvider

## Config keys

- Provider: "Teams" (or "TeamsProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Teams incoming webhook URL

## How to get WebhookUrl

1. Log in to Microsoft Teams.
2. Open the target team and channel.
3. Open the channel menu -> Connectors (or Manage channel -> Connectors).
4. Add an Incoming Webhook connector and copy the URL.

You need permission to add connectors to the channel.

## Example

```json
{
  "Provider": "Teams",
  "Alias": "ops",
  "WebhookUrl": "https://...webhook.office.com/..."
}
```

## SendOptions

Use `TeamsSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` (Markdown is stripped to plain text for Teams cards) |
| Title | `string` | "Notification" | Custom message card title |
| Summary | `string` | "Notification" | Short text summary for the message card |
| ThemeColor | `System.Drawing.Color` | `#6c757d` | Card theme color (use `ColorTranslator.FromHtml("#rrggbb")` to set) |

## References

- https://learn.microsoft.com/en-us/microsoftteams/platform/webhooks-and-connectors/how-to/add-incoming-webhook
