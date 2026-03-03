# WebhookProvider

## Config keys

- Provider: "Webhook" (or "WebhookProvider")
- Alias: optional string, defaults to "default"
- Url: target HTTP endpoint URL
- Method: HTTP method (default "POST")
- Headers: dictionary of custom HTTP headers
- BodyTemplate: optional custom body template, supports `{message}` and `{level}` placeholders. If omitted, sends `{"text":"<emoji> <message>"}`

## Usage notes

This is a generic HTTP webhook provider. Use it to integrate with any service
that accepts HTTP requests — CI/CD pipelines, custom APIs, automation platforms
(Zapier, n8n, IFTTT), or any internal endpoint.

In BodyTemplate you can use:
- `{message}` — replaced with the notification message
- `{level}` — replaced with the level name (Success, Info, Warning, Error, Critical)

## Example

```json
{
  "Provider": "Webhook",
  "Alias": "custom-api",
  "Url": "https://my-api.example.com/alert",
  "Method": "POST",
  "Headers": {
    "X-Api-Key": "secret-key",
    "X-Source": "LNotification"
  },
  "BodyTemplate": "{\"text\": \"{message}\", \"severity\": \"{level}\"}"
}
```

Minimal example (no custom body):

```json
{
  "Provider": "Webhook",
  "Url": "https://hooks.example.com/notify"
}
```
