namespace Shiny.Notifications;

/// <summary>
/// The sound profile used by a notification channel.
/// </summary>
public enum ChannelSound
{
    /// <summary>
    /// No sound is played when a notification on this channel arrives.
    /// </summary>
    None,

    /// <summary>
    /// The system default notification sound is played.
    /// </summary>
    Default,

    /// <summary>
    /// A more attention-grabbing/loud system sound is played where supported.
    /// </summary>
    High,

    /// <summary>
    /// A custom sound is played, sourced from <see cref="Channel.CustomSoundPath"/>.
    /// </summary>
    Custom
}
