# GotifyProvider

## Config keys

- Provider: "Gotify" (or "GotifyProvider")
- Alias: optional string, defaults to "default"
- ServerUrl: Gotify server URL (required)
- Token: application token (required)

## How to get Token

1. Log in to your Gotify web UI.
2. Go to Apps → Create Application.
3. Copy the generated application token.

## Example

```json
{
  "Provider": "Gotify",
  "Alias": "homelab",
  "ServerUrl": "https://gotify.my-server.com",
  "Token": "A_xxxxxxxx"
}
```

## Notes

- Markdown content is sent with `contentType: text/markdown` via Gotify extras, so clients that support it will render rich text.

## SendOptions

Use `GotifySendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` |
| Title | `string` | "Notification" | Custom notification title |
| Priority | `int?` | 3 | Priority 1–5 (1=min, 5=max). Note: the library maps 1–5 to Gotify API 0–10 (1→0, 5→10) |

## References

- https://gotify.net
- https://gotify.net/docs/pushmsg
