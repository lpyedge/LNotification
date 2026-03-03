namespace LNotification.Internal;

/// <summary>
/// Message body format used by provider send options.
/// </summary>
public enum MessageContentFormat
{
    PlainText,
    Markdown
}

/// <summary>
/// Marker interface for providers that accept a specific send options type.
/// </summary>
/// <typeparam name="TOptions">Provider send options type.</typeparam>
public interface INotificationProvider<TOptions>
    where TOptions : SendOptions
{
}

public interface IProviderSendOptions<TOptions>
    where TOptions : SendOptions, new()
{
    TOptions SendOptions { get; set; }
}

/// <summary>
/// Base class for provider-specific per-message send options.
/// </summary>
public abstract class SendOptions
{
    /// <summary>
    /// Message body format. Providers decide how to render Markdown.
    /// </summary>
    public MessageContentFormat ContentFormat { get; set; } = MessageContentFormat.PlainText;
}
