using System;
using System.Collections.Generic;
using System.Linq;


namespace Shiny.Notifications
{
    public class Channel
    {
        public static Channel Default { get; } = new Channel
        {
            Identifier = "Notifications",
            Importance = ChannelImportance.Low
        };


        public string Identifier { get; set; }
        public string? Description { get; set; }
        public List<ChannelAction> Actions { get; set; } = new List<ChannelAction>();

        public ChannelImportance Importance { get; set; } = ChannelImportance.Normal;
        public string? CustomSoundPath { get; set; }


        public static Channel Create(string id, params ChannelAction[] actions)
        {
            var channel = new Channel { Identifier = id };
            if (actions.Length > 0)
                channel.Actions = actions.ToList();

            return channel;
        }
    }
}
