using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Pushover built-in notification sounds.</summary>
public enum PushoverSound
{
    /// <summary>Use user's default sound setting.</summary>
    Default,
    /// <summary>Pushover (default tone).</summary>
    Pushover,
    /// <summary>Bike bell.</summary>
    Bike,
    /// <summary>Bugle call.</summary>
    Bugle,
    /// <summary>Cash register cha-ching.</summary>
    CashRegister,
    /// <summary>Classical melody.</summary>
    Classical,
    /// <summary>Cosmic sound effect.</summary>
    Cosmic,
    /// <summary>Falling sound effect.</summary>
    Falling,
    /// <summary>Gamelan percussion.</summary>
    Gamelan,
    /// <summary>Incoming transmission.</summary>
    Incoming,
    /// <summary>Intermission tone.</summary>
    Intermission,
    /// <summary>Magic sparkle sound.</summary>
    Magic,
    /// <summary>Mechanical click.</summary>
    Mechanical,
    /// <summary>Piano bar melody.</summary>
    PianoBar,
    /// <summary>Emergency siren.</summary>
    Siren,
    /// <summary>Space alarm.</summary>
    SpaceAlarm,
    /// <summary>Tug boat horn.</summary>
    TugBoat,
    /// <summary>Alien alarm (long duration).</summary>
    Alien,
    /// <summary>Climb tone (long duration).</summary>
    Climb,
    /// <summary>Persistent alert (long duration).</summary>
    Persistent,
    /// <summary>Pushover Echo (long duration).</summary>
    Echo,
    /// <summary>Up Down tone (long duration).</summary>
    UpDown,
    /// <summary>Vibrate only, no sound.</summary>
    Vibrate,
    /// <summary>Completely silent, no vibration.</summary>
    None
}

/// <summary>Pushover message body format.</summary>
public enum PushoverMessageFormat
{
    /// <summary>Plain text body (default).</summary>
    PlainText,
    /// <summary>HTML formatting (&lt;b&gt;, &lt;i&gt;, &lt;u&gt;, &lt;font&gt;, &lt;a&gt;).</summary>
    Html,
    /// <summary>Monospace font. Cannot be combined with HTML.</summary>
    Monospace
}

public sealed class PushoverProvider : NotificationProviderBase<PushoverProvider.PushoverConfig, PushoverProvider.PushoverSendOptions>
{
    private const string PushoverApiUrl = "https://api.pushover.net/1/messages.json";

    /// <summary>
    /// Per-message options for Pushover notifications.
    /// </summary>
    public sealed class PushoverSendOptions : SendOptions
    {
        /// <summary>Override message priority (1-5, default 3).</summary>
        public int? Priority { get; set; } = 3;

        /// <summary>Override notification sound.</summary>
        public PushoverSound Sound { get; set; } = PushoverSound.Default;

        /// <summary>Message body format (plain text, HTML, or monospace).</summary>
        public PushoverMessageFormat Format { get; set; } = PushoverMessageFormat.PlainText;

        /// <summary>Optional title for the notification. Defaults to "Notification".</summary>
        public string Title { get; set; } = "Notification";

        /// <summary>Target specific device name (as configured in your Pushover app).</summary>
        public string? Device { get; set; }

        /// <summary>Supplementary URL shown with the message.</summary>
        public string? Url { get; set; }

        /// <summary>Display title for the supplementary URL.</summary>
        public string? UrlTitle { get; set; }

        /// <summary>Message time-to-live in seconds. After this duration, the message is automatically deleted.</summary>
        public int? Ttl { get; set; }
    }

    public sealed class PushoverConfig : ProviderConfigBase, IProviderSendOptions<PushoverSendOptions>
    {
        public string ApplicationToken { get; set; } = string.Empty;
        public string UserKey { get; set; } = string.Empty;
        
        public PushoverSendOptions SendOptions { get; set; } = new();
    }

    internal PushoverProvider(
        IHttpClientFactory factory,
        ILogger<PushoverProvider> logger,
        NotificationOptions options)
        : base(factory, logger, options) { }

    protected override async Task SendInternalAsync(
        PushoverConfig config,
        string message,
        PushoverSendOptions options)
    {
        var payload = options.ContentFormat == MessageContentFormat.Markdown
            ? BuildPayload(config, RegexPatterns.MarkdownToHtml(message), options)
            : BuildPayload(config, message, options);

        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            payload["html"] = "1";
        }
        else
        {
            ApplyFormat(payload, options);
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        using var response = await client.PostAsync(
            PushoverApiUrl,
            new FormUrlEncodedContent(payload.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))));
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static Dictionary<string, string> BuildPayload(
        PushoverConfig c,
        string message,
        PushoverSendOptions o)
    {
        var priority = ResolvePriority(o.Priority);
        var payload = new Dictionary<string, string>
        {
            ["token"] = c.ApplicationToken,
            ["user"] = c.UserKey,
            ["title"] = string.IsNullOrWhiteSpace(o.Title) ? "Notification" : o.Title,
            ["message"] = message,
            ["priority"] = priority.ToString()
        };

        // Sound
        var soundStr = ResolveSoundString(o.Sound);
        if (soundStr != null)
        {
            payload["sound"] = soundStr;
        }

        if (!string.IsNullOrWhiteSpace(o.Device))
            payload["device"] = o.Device!;

        if (!string.IsNullOrWhiteSpace(o.Url))
            payload["url"] = o.Url!;

        if (!string.IsNullOrWhiteSpace(o.UrlTitle))
            payload["url_title"] = o.UrlTitle!;

        if (o.Ttl != null)
            payload["ttl"] = o.Ttl.Value.ToString();

        return payload;
    }

    private static void ApplyFormat(Dictionary<string, string> payload, PushoverSendOptions? o)
    {
        if (o == null) return;

        switch (o.Format)
        {
            case PushoverMessageFormat.Html:
                payload["html"] = "1";
                break;
            case PushoverMessageFormat.Monospace:
                payload["monospace"] = "1";
                break;
        }
    }

    private static string? ResolveSoundString(PushoverSound enumSound)
    {
        if (enumSound != PushoverSound.Default)
        {
            return enumSound switch
            {
                PushoverSound.Pushover => "pushover",
                PushoverSound.Bike => "bike",
                PushoverSound.Bugle => "bugle",
                PushoverSound.CashRegister => "cashregister",
                PushoverSound.Classical => "classical",
                PushoverSound.Cosmic => "cosmic",
                PushoverSound.Falling => "falling",
                PushoverSound.Gamelan => "gamelan",
                PushoverSound.Incoming => "incoming",
                PushoverSound.Intermission => "intermission",
                PushoverSound.Magic => "magic",
                PushoverSound.Mechanical => "mechanical",
                PushoverSound.PianoBar => "pianobar",
                PushoverSound.Siren => "siren",
                PushoverSound.SpaceAlarm => "spacealarm",
                PushoverSound.TugBoat => "tugboat",
                PushoverSound.Alien => "alien",
                PushoverSound.Climb => "climb",
                PushoverSound.Persistent => "persistent",
                PushoverSound.Echo => "echo",
                PushoverSound.UpDown => "updown",
                PushoverSound.Vibrate => "vibrate",
                PushoverSound.None => "none",
                _ => null
            };
        }

        return null;
    }

    private static int ResolvePriority(int? p)
    {
        // Pushover API priority is -2..2. We expose a consistent 1..5 scale.
        var val = p ?? 3;
        if (val < 1) val = 1;
        if (val > 5) val = 5;

        // 1->-2, 2->-1, 3->0, 4->1, 5->2
        return val - 3;
    }
}
