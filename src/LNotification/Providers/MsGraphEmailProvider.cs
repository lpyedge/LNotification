using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Microsoft Graph email importance level.</summary>
public enum EmailImportance
{
    /// <summary>Low importance.</summary>
    Low,
    /// <summary>Normal importance (default).</summary>
    Normal,
    /// <summary>High importance. Flagged with exclamation mark in most email clients.</summary>
    High
}

/// <summary>
/// Microsoft Graph API email provider using OAuth2 client credentials flow.
/// Replaces SMTP for enterprise mailboxes that require modern authentication.
/// 
/// Required Azure AD App Registration setup:
///   1. Register an app in Azure AD (Entra ID)
///   2. Add API permission: Microsoft Graph → Application → Mail.Send
///   3. Grant admin consent
///   4. Create a client secret
/// </summary>
public sealed class MsGraphEmailProvider : NotificationProviderBase<MsGraphEmailProvider.MsGraphEmailConfig, MsGraphEmailProvider.MsGraphEmailSendOptions>
{
    private const string GraphSendMailUrl = "https://graph.microsoft.com/v1.0/users/{0}/sendMail";
    private const string TokenEndpointTemplate = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

    /// <summary>
    /// Per-message options for Microsoft Graph email notifications.
    /// </summary>
    public sealed class MsGraphEmailSendOptions : SendOptions
    {
        /// <summary>Override email subject. If null, uses default "[SubjectPrefix] [Level]".</summary>
        public string? Subject { get; set; }

        /// <summary>Reply-To email address.</summary>
        public string? ReplyTo { get; set; }

        /// <summary>Email importance level.</summary>
        public EmailImportance Importance { get; set; } = EmailImportance.Normal;
    }

    public sealed class MsGraphEmailConfig : ProviderConfigBase, IProviderSendOptions<MsGraphEmailSendOptions>
    {
        /// <summary>Azure AD tenant ID (GUID or domain)</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Azure AD app registration client ID</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Azure AD app registration client secret</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>Sender email address (must be a valid mailbox the app has Mail.Send permission for)</summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>Sender display name (optional)</summary>
        public string FromDisplayName { get; set; } = string.Empty;

        /// <summary>Recipient email addresses</summary>
        public List<string> To { get; set; } = new();

        /// <summary>CC recipients (optional)</summary>
        public List<string>? Cc { get; set; }

        /// <summary>BCC recipients (optional)</summary>
        public List<string>? Bcc { get; set; }

        /// <summary>Subject prefix added to all emails</summary>
        public string SubjectPrefix { get; set; } = "[Notify]";

        /// <summary>Save sent message to Sent Items folder (default: false)</summary>
        public bool SaveToSentItems { get; set; } = false;

        public MsGraphEmailSendOptions SendOptions { get; set; } = new();
    }

    // Token cache — per provider instance (re-created on config reload)
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    internal MsGraphEmailProvider(
        IHttpClientFactory factory,
        ILogger<MsGraphEmailProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        MsGraphEmailConfig config,
        string message,
        NotificationService.NotifyLevel level,
        MsGraphEmailSendOptions options)
    {
        var subject = options.Subject ?? $"{config.SubjectPrefix} [{level}]";

        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            var htmlBody = RegexPatterns.MarkdownToHtml(message);
            await SendGraphEmailAsync(config, subject, htmlBody, isHtml: true, options);
            return;
        }

        await SendGraphEmailAsync(config, subject, message, isHtml: false, options);
    }

    private async Task SendGraphEmailAsync(
        MsGraphEmailConfig config,
        string subject,
        string body,
        bool isHtml,
        MsGraphEmailSendOptions options)
    {
        if (config.To == null || config.To.Count == 0)
        {
            throw new InvalidOperationException("MsGraphEmailConfig.To cannot be empty");
        }

        var token = await GetAccessTokenAsync(config);
        var client = HttpClientFactory.CreateClient(NotificationHttpClient);

        var url = string.Format(GraphSendMailUrl, config.FromAddress);

        var toRecipients = new List<object>();
        foreach (var addr in config.To)
        {
            if (!string.IsNullOrWhiteSpace(addr))
            {
                toRecipients.Add(new { emailAddress = new { address = addr } });
            }
        }

        var ccRecipients = new List<object>();
        if (config.Cc?.Count > 0)
        {
            foreach (var addr in config.Cc)
            {
                if (!string.IsNullOrWhiteSpace(addr))
                {
                    ccRecipients.Add(new { emailAddress = new { address = addr } });
                }
            }
        }

        var bccRecipients = new List<object>();
        if (config.Bcc?.Count > 0)
        {
            foreach (var addr in config.Bcc)
            {
                if (!string.IsNullOrWhiteSpace(addr))
                {
                    bccRecipients.Add(new { emailAddress = new { address = addr } });
                }
            }
        }

        var replyToList = new List<object>();
        if (!string.IsNullOrWhiteSpace(options.ReplyTo))
        {
            replyToList.Add(new { emailAddress = new { address = options.ReplyTo } });
        }

        var importanceStr = options.Importance switch
        {
            EmailImportance.Low => "low",
            EmailImportance.High => "high",
            _ => "normal"
        };

        var payload = new
        {
            message = new
            {
                subject = subject,
                body = new
                {
                    contentType = isHtml ? "HTML" : "Text",
                    content = body
                },
                from = new
                {
                    emailAddress = new
                    {
                        address = config.FromAddress,
                        name = string.IsNullOrWhiteSpace(config.FromDisplayName)
                            ? config.FromAddress
                            : config.FromDisplayName
                    }
                },
                toRecipients = toRecipients,
                ccRecipients = ccRecipients,
                bccRecipients = bccRecipients,
                replyTo = replyToList,
                importance = importanceStr
            },
            saveToSentItems = config.SaveToSentItems
        };

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response, config.Alias);
    }

    private async Task<string> GetAccessTokenAsync(MsGraphEmailConfig config)
    {
        // Check cached token (with 5-minute buffer before expiry)
        if (_cachedToken != null && DateTime.UtcNow.AddMinutes(5) < _tokenExpiry)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken != null && DateTime.UtcNow.AddMinutes(5) < _tokenExpiry)
            {
                return _cachedToken;
            }

            var tokenUrl = string.Format(TokenEndpointTemplate, config.TenantId);
            var tokenPayload = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = config.ClientId,
                ["client_secret"] = config.ClientSecret,
                ["scope"] = "https://graph.microsoft.com/.default"
            };

            var httpClient = HttpClientFactory.CreateClient(NotificationHttpClient);
            var response = await httpClient.PostAsync(
                tokenUrl,
                new FormUrlEncodedContent(tokenPayload.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError(
                    "MsGraphEmail token request failed with {StatusCode}: {Content}",
                    response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            var tokenResponse = await response.Content.ReadAsStringAsync();

            // Simple JSON parsing without extra dependencies
            var accessToken = ExtractJsonValue(tokenResponse, "access_token");
            var expiresIn = ExtractJsonValue(tokenResponse, "expires_in");

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Failed to obtain access token from Azure AD");
            }

            _cachedToken = accessToken;
            _tokenExpiry = int.TryParse(expiresIn, out var seconds)
                ? DateTime.UtcNow.AddSeconds(seconds)
                : DateTime.UtcNow.AddMinutes(55); // Default ~1 hour

            Logger.LogDebug("MsGraphEmail token acquired, expires at {Expiry}", _tokenExpiry);
            return accessToken!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Minimal JSON value extractor — avoids dependency on System.Text.Json or Newtonsoft
    /// for netstandard2.0 compatibility. Only handles flat string/number values.
    /// </summary>
    private static string? ExtractJsonValue(string json, string key)
    {
        var searchKey = $"\"{key}\"";
        var keyIndex = json.IndexOf(searchKey, StringComparison.Ordinal);
        if (keyIndex < 0) return null;

        var colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
        if (colonIndex < 0) return null;

        var valueStart = colonIndex + 1;
        // Skip whitespace
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;

        if (valueStart >= json.Length) return null;

        if (json[valueStart] == '"')
        {
            // String value
            var valueEnd = json.IndexOf('"', valueStart + 1);
            return valueEnd > valueStart
                ? json.Substring(valueStart + 1, valueEnd - valueStart - 1)
                : null;
        }
        else
        {
            // Number or other non-quoted value
            var valueEnd = valueStart;
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}' && !char.IsWhiteSpace(json[valueEnd]))
                valueEnd++;

            return json.Substring(valueStart, valueEnd - valueStart);
        }
    }
}
