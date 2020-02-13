using System;
using System.Collections.Generic;
using Shiny.Push;


namespace Shiny.Testing.Push
{
    public class TestPushNotification : IPushNotification
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Category { get; set; }
        public int Badge { get; set; }
        public bool IsContentAvailable { get; set; }
        public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
