using System;
#if ANDROID
using Android.App;
#endif

namespace Shiny.Push;


public record FirebaseConfiguration(
    /// <summary>
    /// If you have included a GoogleService-Info.plist (iOS) or google-services.json (Android)
    /// </summary>
    bool UseEmbeddedConfiguration = true,

    string? AppId = null,
    string? SenderId = null,
    string? ProjectId = null,
    string? ApiKey = null
#if ANDROID
    , NotificationChannel? DefaultChannel = null
    , string? IntentAction = null
#endif
)
{
    public static FirebaseConfiguration Embedded { get; } = new(true);


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
