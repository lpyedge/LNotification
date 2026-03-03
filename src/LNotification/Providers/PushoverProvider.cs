using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LNotification.Internal;

namespace LNotification.Providers;

/// <summary>Pushover message priority (-2 to 2).</summary>
public enum PushoverPriority
{
    /// <summary>Lowest (-2). No notification generated, only badge count increase on iOS.</summary>
    Lowest = -2,
    /// <summary>Low (-1). No sound or vibration, popup/scroll notification only.</summary>
    Low = -1,
    /// <summary>Normal (0). Standard sound, vibration, and alert per user settings.</summary>
    Normal = 0,
    /// <summary>High (1). Bypasses quiet hours. Always plays sound and vibrates.</summary>
    High = 1,
    /// <summary>Emergency (2). Repeated until acknowledged by user. Requires Retry and Expire on server.</summary>
    Emergency = 2
}

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
        /// <summary>Override message priority for this notification.</summary>
        public PushoverPriority? Priority { get; set; }

        /// <summary>Override notification sound.</summary>
        public PushoverSound Sound { get; set; } = PushoverSound.Default;

        /// <summary>Message body format (plain text, HTML, or monospace).</summary>
        public PushoverMessageFormat Format { get; set; } = PushoverMessageFormat.PlainText;

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
        public int Priority { get; set; } = 0;
        public string? Sound { get; set; }
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
        NotificationService.NotifyLevel level,
        PushoverSendOptions options)
    {
        var payload = options.ContentFormat == MessageContentFormat.Markdown
            ? BuildPayload(config, RegexPatterns.MarkdownToHtml(message), level, options)
            : BuildPayload(config, message, level, options);

        if (options.ContentFormat == MessageContentFormat.Markdown)
        {
            payload["html"] = "1";
        }
        else
        {
            ApplyFormat(payload, options);
        }

        var client = HttpClientFactory.CreateClient(NotificationHttpClient);
        var response = await client.PostAsync(
            PushoverApiUrl,
            new FormUrlEncodedContent(payload.Select(kv => new KeyValuePair<string?, string?>(kv.Key, kv.Value))));
        await EnsureSuccessAsync(response, config.Alias);
    }

    private static Dictionary<string, string> BuildPayload(
        PushoverConfig c,
        string message,
        NotificationService.NotifyLevel level,
        PushoverSendOptions o)
    {
        var priority = o.Priority != null ? (int)o.Priority : c.Priority;
        var payload = new Dictionary<string, string>
        {
            ["token"] = c.ApplicationToken,
            ["user"] = c.UserKey,
            ["title"] = $"{Emoji(level)} [{level}]",
            ["message"] = message,
            ["priority"] = priority.ToString()
        };

        // Sound
        var soundStr = ResolveSoundString(o.Sound, c.Sound);
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

    private static string? ResolveSoundString(PushoverSound enumSound, string? configSound)
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

        // Fall back to config-level sound string
        return !string.IsNullOrWhiteSpace(configSound) ? configSound : null;
    }
}
