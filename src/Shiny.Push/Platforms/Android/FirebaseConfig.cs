using System;


namespace Shiny.Push
{
    public class FirebaseConfig
    {
        public FirebaseConfig(string appId, string projectId, string apiKey, string? appName)
        {
            this.AppId = appId ?? throw new ArgumentNullException(nameof(appId));
            this.ProjectId = projectId ?? throw new ArgumentNullException(nameof(projectId));
            this.ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            this.AppName = appName;
        }


        public string? AppName { get; set; }
        public string AppId { get; }
        public string ProjectId { get; }
        public string ApiKey { get; }
    }
}
