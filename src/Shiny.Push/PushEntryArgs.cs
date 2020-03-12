using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    public class PushEntryArgs
    {
        public PushEntryArgs(string? categoryIdentifier,
                             string? actionIdentifier,
                             string? textReply,
                             IDictionary<string, string> userInfo)
        {
            this.CategoryIdentifier = categoryIdentifier;
            this.ActionIdentifier = actionIdentifier;
            this.TextReply = textReply;
            this.UserInfo = userInfo;
        }

        public string? CategoryIdentifier { get; }
        public string? ActionIdentifier { get; }
        public string? TextReply { get; }
        public IDictionary<string, string> UserInfo { get; }
    }
}
