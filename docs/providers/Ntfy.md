# NtfyProvider

## Config keys

- Provider: "Ntfy" (or "NtfyProvider")
- Alias: optional string, defaults to "default"
- ServerUrl: ntfy server URL (default "https://ntfy.sh")
- Topic: ntfy topic name (required)
- Token: optional access token for authentication
- Priority: message priority 1–5 (default 3, where 1=min, 5=max)

## How to get Topic

1. Go to https://ntfy.sh (or your self-hosted ntfy instance).
2. Choose a topic name (any unique string, e.g. "my-alerts").
3. Subscribe to the topic on your phone (ntfy app) or desktop.

For self-hosted ntfy, set ServerUrl to your instance URL.

## Example

```json
{
  "Provider": "Ntfy",
  "Alias": "ops",
  "ServerUrl": "https://ntfy.sh",
  "Topic": "my-alerts",
  "Token": "tk_xxxxxxxx",
  "Priority": 4
}
```

Public topic (no auth):

```json
{
  "Provider": "Ntfy",
  "Topic": "my-public-topic"
}
```

## SendOptions

Use `NtfySendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| Priority | `NtfyPriority?` | null | `Min(1)` / `Low(2)` / `Default(3)` / `High(4)` / `Urgent(5)` |
| Tags | `string?` | null | Comma-separated emoji tags (e.g. "warning,skull") |
| ClickUrl | `string?` | null | URL opened on notification click |

## References

- https://ntfy.sh
- https://docs.ntfy.sh/publish/
- https://docs.ntfy.sh/publish/#access-tokens
