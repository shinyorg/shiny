using System.Collections.Generic;
using System.Linq;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


public class Channel : IRepositoryEntity
{
    public static Channel Default { get; } = new Channel
    {
        Identifier = "Notifications",
        Importance = ChannelImportance.Low
    };


    public string Identifier { get; set; } = null!;
    public string? Description { get; set; }
    public List<ChannelAction> Actions { get; set; } = new();

    public ChannelImportance Importance { get; set; } = ChannelImportance.Normal;

    public ChannelSound Sound { get; set; } = ChannelSound.Default;
    public string? CustomSoundPath { get; set; }


    public static Channel Create(string id, params ChannelAction[] actions)
    {
        var channel = new Channel { Identifier = id };
        if (actions.Length > 0)
            channel.Actions = actions.ToList();

        return channel;
    }
}
