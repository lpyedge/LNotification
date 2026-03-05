# EmailProvider

## Config keys

- Provider: "Email" (or "EmailProvider")
- Alias: optional string, defaults to "default"
- SmtpHost: SMTP server hostname
- SmtpPort: SMTP port (e.g. 587 for STARTTLS, 465 for SSL)
- EnableSsl: true to enable TLS
- FromAddress: sender email address
- FromDisplayName: optional display name
- Username: SMTP username (often the email address)
- Password: SMTP password or app password
- To: list of recipient addresses
- Cc: optional list of CC addresses
- Bcc: optional list of BCC addresses

## How to get SMTP settings

1. Log in to your email provider's admin or account portal.
2. Find the SMTP server settings (host, port, and TLS requirements).
3. If the provider requires an app password, generate one and use it as Password.

Different providers have different SMTP endpoints and security requirements. Use the
provider's official documentation for exact values.

## Example

```json
{
  "Provider": "Email",
  "Alias": "smtp",
  "SmtpHost": "smtp.example.com",
  "SmtpPort": 587,
  "EnableSsl": true,
  "FromAddress": "noreply@example.com",
  "FromDisplayName": "Notifier",
  "Username": "smtp-user",
  "Password": "smtp-pass",
  "To": ["ops@example.com"],
  "Cc": ["dev@example.com"],
  "Bcc": ["audit@example.com"]
}
```

## SendOptions

Use `EmailSendOptions` to customize individual messages:

| Property | Type | Default | Description |
|---|---|---|---|
| ContentFormat | `MessageContentFormat` | PlainText | `PlainText` / `Markdown` |
| Title | `string` | "Notification" | Override email title/subject line |
| ReplyTo | `string?` | null | Reply-To email address |
| BodyFormat | `EmailBodyFormat` | PlainText | `PlainText` / `Html` |

```csharp
await service.SendAsync<EmailProvider, EmailProvider.EmailSendOptions>(
    "<b>Alert!</b> Server is down.",
    o =>
    {
        o.Title = "Urgent: Server Down";
        o.ReplyTo = "ops@example.com";
        o.BodyFormat = EmailBodyFormat.Html;
    });
```

## References
  
- https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient
