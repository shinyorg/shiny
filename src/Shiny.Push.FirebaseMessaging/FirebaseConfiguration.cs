using System;


namespace Shiny.Push
{
    public class FirebaseConfiguration
    {
        public FirebaseConfiguration(string appId, string senderId, string apiKey)
        {
            this.AppId = appId ?? throw new ArgumentNullException(nameof(appId));
            this.SenderId = apiKey ?? throw new ArgumentNullException(nameof(senderId));
            this.ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }


        public string AppId { get; }
        public string SenderId { get; }
        public string ApiKey { get; }
    }
}
