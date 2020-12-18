using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    public class PushEntryArgs
    {
        public PushEntryArgs(string? channel,
                             string? actionIdentifier,
                             string? textReply,
                             IDictionary<string, string> userInfo)
        {
            this.Channel = channel;
            this.ActionIdentifier = actionIdentifier;
            this.TextReply = textReply;
            this.UserInfo = userInfo;
        }

        public string? Channel { get; }
        public string? ActionIdentifier { get; }
        public string? TextReply { get; }
        public IDictionary<string, string> UserInfo { get; }
    }
}
