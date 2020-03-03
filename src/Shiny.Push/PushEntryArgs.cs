using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    public class PushEntryArgs
    {

        public string? CategoryIdentifier { get; }
        public string? ActionIdentifier { get; }
        public string? TextReply { get; }
        public IDictionary<string, string> UserInfo { get; }
    }
}
