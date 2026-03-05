using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Email body format.</summary>
public enum EmailBodyFormat
{
    /// <summary>Plain text email body.</summary>
    PlainText,
    /// <summary>HTML formatted email body.</summary>
    Html
}

public sealed class EmailProvider : NotificationProviderBase<EmailProvider.EmailConfig, EmailProvider.EmailSendOptions>
{
    /// <summary>
    /// Per-message options for SMTP email notifications.
    /// </summary>
    public sealed class EmailSendOptions : SendOptions
    {
        /// <summary>Override email title (subject line).</summary>
        public string Title { get; set; } = "Notification";

        /// <summary>Reply-To email address.</summary>
        public string? ReplyTo { get; set; }

        /// <summary>Body format for plain text content. Ignored when ContentFormat is Markdown.</summary>
        public EmailBodyFormat BodyFormat { get; set; } = EmailBodyFormat.PlainText;
    }

    public sealed class EmailConfig : ProviderConfigBase, IProviderSendOptions<EmailSendOptions>
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;

        public string FromAddress { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = string.Empty;

        public string? Username { get; set; }
        public string? Password { get; set; }

        public List<string> To { get; set; } = new();
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }

        public EmailSendOptions SendOptions { get; set; } = new();
    }

    internal EmailProvider(
        IHttpClientFactory factory,
        ILogger<EmailProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override Task SendInternalAsync(
        EmailConfig config,
        string message,
        EmailSendOptions options)
    {
        var subject = options.Title;

        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            var htmlBody = RegexPatterns.MarkdownToHtml(message);
            return SendEmailAsync(config, subject, htmlBody, isHtml: true, options.ReplyTo);
        }

        var isHtml = options.BodyFormat == EmailBodyFormat.Html;
        return SendEmailAsync(config, subject, message, isHtml, options.ReplyTo);
    }

    private async Task SendEmailAsync(
        EmailConfig config,
        string subject,
        string body,
        bool isHtml,
        string? replyTo = null)
    {
        if (config.To == null || config.To.Count == 0)
        {
            throw new InvalidOperationException("EmailConfig.To cannot be empty");
        }

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(
                config.FromAddress,
                string.IsNullOrWhiteSpace(config.FromDisplayName)
                    ? config.FromAddress
                    : config.FromDisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };

        foreach (var to in config.To)
        {
            if (!string.IsNullOrWhiteSpace(to))
            {
                mailMessage.To.Add(to);
            }
        }

        if (config.Cc?.Count > 0)
        {
            foreach (var cc in config.Cc)
            {
                if (!string.IsNullOrWhiteSpace(cc)) mailMessage.CC.Add(cc);
            }
        }

        if (config.Bcc?.Count > 0)
        {
            foreach (var bcc in config.Bcc)
            {
                if (!string.IsNullOrWhiteSpace(bcc)) mailMessage.Bcc.Add(bcc);
            }
        }

        if (!string.IsNullOrWhiteSpace(replyTo))
        {
            mailMessage.ReplyToList.Add(new MailAddress(replyTo));
        }

        using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
        {
            EnableSsl = config.EnableSsl,
            Timeout = TimeoutSeconds * 1000
        };

        if (!string.IsNullOrWhiteSpace(config.Username) &&
            !string.IsNullOrWhiteSpace(config.Password))
        {
            client.Credentials = new NetworkCredential(
                config.Username,
                config.Password);
        }
        else
        {
            client.UseDefaultCredentials = true;
        }

        await client.SendMailAsync(mailMessage);
    }
}
