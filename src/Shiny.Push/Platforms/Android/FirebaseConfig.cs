using System;
using Android.App;

namespace Shiny.Push;


public record FirebaseConfig(
    bool UseEmbeddedConfiguration = true,
    string? AppId = null,
    string? SenderId = null,
    string? ProjectId = null,
    string? ApiKey = null,

    NotificationChannel? DefaultChannel = null,
    string? IntentAction = null
)
{
    public static FirebaseConfig Embedded { get; } = new(true);
    public static FirebaseConfig FromValues(string appId, string senderId, string projectId, string apiKey)
    {
        var cfg = new FirebaseConfig(false, appId, senderId, projectId, apiKey);
        cfg.AssertValid();
        return cfg;
    }


    public void AssertValid()
    {
        if (this.UseEmbeddedConfiguration)
            return;

        if (this.AppId == null)
            throw new ArgumentNullException(nameof(this.AppId));

        if (this.SenderId == null)
            throw new ArgumentNullException(nameof(this.SenderId));

        if (this.ApiKey == null)
            throw new ArgumentNullException(nameof(this.ApiKey));

        if (this.ProjectId == null)
            throw new ArgumentNullException(nameof(this.ProjectId));
    }
}