using System.Collections.Generic;
using System.Linq;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


/// <summary>
/// Describes a notification channel. Channels group notifications by purpose and define their importance, sound, and available actions.
/// </summary>
public class Channel : IRepositoryEntity
{
    /// <summary>
    /// A built-in default channel ("Notifications") with low importance used when no channel is specified.
    /// </summary>
    public static Channel Default { get; } = new Channel
    {
        Identifier = "Notifications",
        Importance = ChannelImportance.Low
    };


    /// <summary>
    /// The unique identifier for this channel.
    /// </summary>
    public string Identifier { get; set; } = null!;

    /// <summary>
    /// An optional human-readable description shown in system notification settings.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The actions (buttons, text reply, etc.) attached to notifications posted on this channel.
    /// </summary>
    public List<ChannelAction> Actions { get; set; } = new();

    /// <summary>
    /// The importance level controlling how prominently the system surfaces notifications on this channel.
    /// </summary>
    public ChannelImportance Importance { get; set; } = ChannelImportance.Normal;

    /// <summary>
    /// The sound profile used for notifications on this channel.
    /// </summary>
    public ChannelSound Sound { get; set; } = ChannelSound.Default;

    /// <summary>
    /// The platform-specific path to a custom sound resource. Required when <see cref="Sound"/> is <see cref="ChannelSound.Custom"/>.
    /// </summary>
    public string? CustomSoundPath { get; set; }


    /// <summary>
    /// Creates a new channel with the given identifier and optional actions.
    /// </summary>
    /// <param name="id">The channel identifier.</param>
    /// <param name="actions">Optional actions to attach to notifications posted on this channel.</param>
    /// <returns>A new <see cref="Channel"/> instance.</returns>
    public static Channel Create(string id, params ChannelAction[] actions)
    {
        var channel = new Channel { Identifier = id };
        if (actions.Length > 0)
            channel.Actions = actions.ToList();

        return channel;
    }
}
