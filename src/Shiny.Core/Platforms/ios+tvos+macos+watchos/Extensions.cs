using System;
using Foundation;


namespace Shiny
{
    public static class Extensions_iOS
    {
        public static void Dispatch(this Action action)
        {
            if (NSThread.Current.IsMainThread)
                action();
            else
                NSRunLoop.Main.BeginInvokeOnMainThread(action);
        }

        public static Guid ToGuid(this NSUuid uuid) => Guid.ParseExact(uuid.AsString(), "d");
        public static NSUuid ToNSUuid(this Guid guid) => new NSUuid(guid.ToString());
    }
}
