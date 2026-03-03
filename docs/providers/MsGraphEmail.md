# MsGraphEmailProvider

## Config keys

- Provider: "MsGraphEmail" (or "MsGraphEmailProvider")
- Alias: optional string, defaults to "default"
- TenantId: Azure AD (Entra ID) tenant ID — GUID or domain
- ClientId: Azure AD app registration client ID
- ClientSecret: Azure AD app registration client secret
- FromAddress: sender mailbox address (must have Mail.Send permission)
- FromDisplayName: optional sender display name
- To: list of recipient email addresses
- Cc: optional list of CC addresses
- Bcc: optional list of BCC addresses
- SubjectPrefix: email subject prefix (default "[Notify]")
- SaveToSentItems: save to Sent Items folder (default false)

## Azure AD setup

1. Go to Azure Portal → Microsoft Entra ID → App registrations → New registration.
2. Name the app (e.g. "LNotification Mail") and register.
3. Go to API permissions → Add a permission → Microsoft Graph → Application permissions → **Mail.Send** → Add.
4. Click **Grant admin consent** for your tenant.
5. Go to Certificates & secrets → New client secret → copy the secret value.
6. Note the **Application (client) ID** and **Directory (tenant) ID** from the Overview page.

## Example

```json
{
  "Provider": "MsGraphEmail",
  "Alias": "office365",
  "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ClientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "ClientSecret": "your-client-secret-value",
  "FromAddress": "noreply@company.com",
  "FromDisplayName": "System Notifier",
  "To": ["admin@company.com", "ops@company.com"],
  "Cc": ["manager@company.com"],
  "SubjectPrefix": "[Alert]",
  "SaveToSentItems": false
}
```

## Notes

- Uses **OAuth2 client credentials flow** — no interactive login required.
- Access token is automatically cached and refreshed (with 5-minute buffer before expiry).
- Markdown content is converted to HTML for email body.
- This replaces SMTP-based email for organizations that have disabled basic/SMTP authentication.

## References

- https://learn.microsoft.com/en-us/graph/api/user-sendmail
- https://learn.microsoft.com/en-us/graph/auth-v2-service
- https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app
