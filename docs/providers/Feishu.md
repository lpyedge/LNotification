# FeishuProvider

## Config keys

- Provider: "Feishu" (or "FeishuProvider")
- Alias: optional string, defaults to "default"
- WebhookUrl: Feishu/Lark bot webhook URL

## How to get WebhookUrl

1. Log in to Feishu/Lark and open the target group chat.
2. Add a Custom Bot to the group.
3. Copy the webhook URL from the bot setup screen.

If you enable "Signature verification" for the bot, this library does not yet support
signing the request. Keep signature verification disabled or add signing support.

## Example

```json
{
  "Provider": "Feishu",
  "Alias": "default",
  "WebhookUrl": "https://open.feishu.cn/open-apis/bot/v2/hook/..."
}
```

## References

- https://open.feishu.cn/document/client-docs/bot-v3/add-custom-bot
