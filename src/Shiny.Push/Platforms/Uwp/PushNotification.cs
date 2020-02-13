using System;
using System.Collections.Generic;


namespace Shiny.Push
{
    class PushNotification : IPushNotification
    {
        public string Title => "TODO";
        public string Body => "TODO";
        public string Category => "TODO";
        public bool IsContentAvailable => false;
        public int Badge => 0;
        
        public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();
    }
}
