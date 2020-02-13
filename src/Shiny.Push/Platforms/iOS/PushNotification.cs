using System;
using System.Collections.Generic;
using Foundation;
using UserNotifications;


namespace Shiny.Push
{
    class PushNotification : IPushNotification
    {
        static readonly NSString apsString = new NSString("aps");
        static readonly NSString contentAvail = new NSString("content-available");

        readonly UNNotification native;


        public PushNotification(UNNotification native)
        {
            this.Data = new Dictionary<string, object>();
            this.native = native;

            var info = this.native.Request.Content.UserInfo;
            foreach (var value in info)
            {
                if (value.Key.Equals(apsString))
                {
                    var aps = value.Value as NSDictionary;
                    if (aps != null)
                    {
                        foreach (var item in aps)
                        {
                            if (item.Key.Equals(contentAvail))
                                this.IsContentAvailable = item.Value.ToString() == "1";
                        }
                    }
                }
                else
                {
                    this.Data.Add(value.Key.ToString(), value.Value);
                }
            }
        }


        public string Title => this.native.Request.Content?.Title;
        public string Body => this.native.Request.Content?.Body;
        public string Category => this.native.Request.Content?.CategoryIdentifier;
        public int Badge => this.native.Request.Content?.Badge?.Int32Value ?? 0;
        public bool IsContentAvailable { get; }
        public IDictionary<string, object> Data { get; }
    }
}
