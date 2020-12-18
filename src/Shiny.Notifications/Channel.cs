using System;
using System.Collections.Generic;


namespace Shiny.Notifications
{
    public class Channel
    {
        public string Identifier { get; set; }
        public string? Description { get; set; }
        public List<ChannelAction> Actions { get; set; } = new List<ChannelAction>();

        public ChannelImportance Importance { get; set; } = ChannelImportance.Normal;
        public string? CustomSoundPath { get; set; }
    }
}
