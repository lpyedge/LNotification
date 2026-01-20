using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

public sealed class EmailProvider : NotificationProviderBase
{
    public sealed class EmailConfig : ProviderConfigBase
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

        public string SubjectPrefix { get; set; } = "[Notify]";
        public bool IsHtml { get; set; } = false;
    }

    internal EmailProvider(
        IHttpClientFactory factory,
        ILogger<EmailProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override Task SendInternalAsync(
        ProviderConfigBase config,
        string message,
        NotificationService.NotifyLevel level)
    {
        var c = (EmailConfig)config;
        var subject = $"{c.SubjectPrefix} [{level}]";
        return SendEmailAsync(c, subject, message, c.IsHtml);
    }

    protected override Task SendMarkdownInternalAsync(
        ProviderConfigBase config,
        string markdownContent,
        NotificationService.NotifyLevel level)
    {
        var c = (EmailConfig)config;
        var subject = $"{c.SubjectPrefix} [{level}]";
        var htmlBody = RegexPatterns.MarkdownToHtml(markdownContent);
        return SendEmailAsync(c, subject, htmlBody, isHtml: true);
    }

    private async Task SendEmailAsync(
        EmailConfig config,
        string subject,
        string body,
        bool isHtml)
    {
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
            mailMessage.To.Add(to);
        }

        if (config.Cc?.Count > 0)
        {
            foreach (var cc in config.Cc)
            {
                mailMessage.CC.Add(cc);
            }
        }

        if (config.Bcc?.Count > 0)
        {
            foreach (var bcc in config.Bcc)
            {
                mailMessage.Bcc.Add(bcc);
            }
        }

        using var client = new SmtpClient(config.SmtpHost, config.SmtpPort)
        {
            EnableSsl = config.EnableSsl,
            Timeout = TimeoutSeconds * 1000
        };

        // 修复：同时检查Username和Password
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
