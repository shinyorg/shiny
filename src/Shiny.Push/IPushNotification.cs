using System;
using System.Collections.Generic;

namespace Shiny.Push
{
    public interface IPushNotification
    {
        public string Title { get; }
        public string Body { get; }
        public int Badge { get; }
        public bool IsContentAvailable { get; }
        public string Category { get; }
        public IDictionary<string, object> Data { get; }
    }
}
