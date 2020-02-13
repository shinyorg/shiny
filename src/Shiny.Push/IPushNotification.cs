using System;


namespace Shiny.Push
{
    public interface IPushNotification
    {
        public string Title { get; }
        public string Body { get; }
    }
}
