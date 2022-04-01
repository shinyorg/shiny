using System;


namespace Shiny.Push
{
    public class FirebaseConfiguration
    {
        public FirebaseConfiguration(string appId, string projectId, string senderId, string apiKey)
        {
            this.AppId = appId ?? throw new ArgumentNullException(nameof(appId));
            this.ProjectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            this.SenderId = senderId ?? throw new ArgumentNullException(nameof(senderId));
            this.ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }


        public string AppId { get; }
        public string SenderId { get; }
        public string ProjectId { get; }
        public string ApiKey { get; }
    }
}
