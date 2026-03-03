# PushoverProvider

## Config keys

- Provider: "Pushover" (or "PushoverProvider")
- Alias: optional string, defaults to "default"
- ApplicationToken: Pushover application API token (required)
- UserKey: Pushover user/group key (required)
- Priority: message priority -2 to 2 (default 0)
- Sound: optional notification sound name

## How to get tokens

1. Create an account at https://pushover.net
2. Your User Key is shown on the dashboard.
3. Go to https://pushover.net/apps/build → create an application → copy the API Token.

## Priority values

| Value | Meaning |
|-------|---------|
| -2 | Lowest (no alert) |
| -1 | Low (quiet) |
| 0 | Normal (default) |
| 1 | High (bypass quiet hours) |
| 2 | Emergency (repeat until acknowledged) |

## Example

```json
{
  "Provider": "Pushover",
  "ApplicationToken": "axxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "UserKey": "uxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "Priority": 0,
  "Sound": "pushover"
}
```

## References

- https://pushover.net/api
- https://pushover.net/api#sounds
