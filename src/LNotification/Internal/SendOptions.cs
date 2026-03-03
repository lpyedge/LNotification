namespace LNotification.Internal;

/// <summary>
/// Base class for provider-specific per-message send options.
/// Each provider defines its own sealed subclass with customization properties.
/// Pass an instance to <see cref="NotificationService.SendAsync{TProvider}"/> or
/// <see cref="NotificationService.SendMarkdownAsync{TProvider}"/> to customize individual messages.
/// </summary>
public abstract class SendOptions { }
