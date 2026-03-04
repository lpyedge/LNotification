# PushoverProvider

## Config keys

- Provider: "Pushover" (or "PushoverProvider")
- Alias: optional string, defaults to "default"
- ApplicationToken: Pushover application API token (required)
- UserKey: Pushover user/group key (required)
 

## How to get tokens

1. Create an account at https://pushover.net
2. Your User Key is shown on the dashboard.
3. Go to https://pushover.net/apps/build → create an application → copy the API Token.

## Priority values
Pushover supports 5 priority levels: Lowest(1), Low(2), Normal(3), High(4), and Emergency(5). The default is Normal(3).

## Example

```json
{
  "Provider": "Pushover",
  "ApplicationToken": "axxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "UserKey": "uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
}
```

## SendOptions

Use `PushoverSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` (`Markdown` is converted to HTML for Pushover) |
| Priority | `int` | 3 | 1–5 where 1=min and 5=max; mapped to Pushover API values (-2..2) by the library |
| Sound | `PushoverSound` | Default | 23 built-in sounds (e.g. `Siren`, `Magic`, `CashRegister`, `Vibrate`, `None`) |
| Format | `PushoverMessageFormat` | PlainText | `PlainText` / `Html` / `Monospace` (used when ContentFormat is `PlainText`) |
| Device | `string?` | null | Target specific device name |
| Url | `string?` | null | Supplementary URL shown with message |
| UrlTitle | `string?` | null | Display title for supplementary URL |
| Ttl | `int?` | null | Time-to-live in seconds |

## References

- https://pushover.net/api
- https://pushover.net/api#sounds
