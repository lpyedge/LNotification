# GotifyProvider

## Config keys

- Provider: "Gotify" (or "GotifyProvider")
- Alias: optional string, defaults to "default"
- ServerUrl: Gotify server URL (required)
- Token: application token (required)
- Priority: message priority (default 5)

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
  "Token": "A_xxxxxxxx",
  "Priority": 5
}
```

## Notes

- Markdown content is sent with `contentType: text/markdown` via Gotify extras, so clients that support it will render rich text.

## References

- https://gotify.net
- https://gotify.net/docs/pushmsg
