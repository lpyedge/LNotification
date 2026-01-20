# SlackProvider

## Config keys

- Provider: "Slack" (or "SlackProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Slack incoming webhook URL

## How to get WebhookUrl

1. Log in to Slack and open the app creation page: https://api.slack.com/apps
2. Create a new app ("From scratch").
3. In the app settings, enable Incoming Webhooks:
   https://api.slack.com/messaging/webhooks
4. Add a new webhook to a workspace/channel and copy the Webhook URL.

You must be logged in and have permission to install apps or manage webhooks for the workspace.

## Example

```json
{
  "Provider": "Slack",
  "Alias": "default",
  "WebhookUrl": "https://hooks.slack.com/services/..."
}
```

## References

- https://api.slack.com/messaging/webhooks
