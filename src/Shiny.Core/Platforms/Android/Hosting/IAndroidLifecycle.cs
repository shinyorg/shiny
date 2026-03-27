using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Shiny.Hosting;


/// <summary>
/// Container for Android lifecycle event interfaces
/// </summary>
public interface IAndroidLifecycle
{
    /// <summary>
    /// Handles application foreground and background transitions
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Called when the application enters the foreground
        /// </summary>
        void OnForeground();

        /// <summary>
        /// Called when the application enters the background
        /// </summary>
        void OnBackground();
    }

    /// <summary>
    /// Handles the Activity OnCreate lifecycle event
    /// </summary>
    public interface IOnActivityOnCreate
    {
        /// <summary>
        /// Called when an activity is created
        /// </summary>
        /// <param name="activity">The activity being created</param>
        /// <param name="savedInstanceState">The saved instance state bundle</param>
        void ActivityOnCreate(Activity activity, Bundle? savedInstanceState);
    }

    /// <summary>
    /// Handles the result of a runtime permission request
    /// </summary>
    public interface IOnActivityRequestPermissionsResult
    {
        /// <summary>
        /// Called when the permission request result is received
        /// </summary>
        /// <param name="activity">The activity that requested permissions</param>
        /// <param name="requestCode">The request code passed to RequestPermissions</param>
        /// <param name="permissions">The requested permissions</param>
        /// <param name="grantResults">The grant results for the permissions</param>
        void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults);
    }

    /// <summary>
    /// Handles new intents delivered to an activity
    /// </summary>
    public interface IOnActivityNewIntent
    {
        /// <summary>
        /// Called when a new intent is delivered to the activity
        /// </summary>
        /// <param name="activity">The activity receiving the intent</param>
        /// <param name="intent">The new intent</param>
        void Handle(Activity activity, Intent intent);
    }

    /// <summary>
    /// Handles activity results from StartActivityForResult
    /// </summary>
    public interface IOnActivityResult
    {
        /// <summary>
        /// Called when an activity result is received
        /// </summary>
        /// <param name="activity">The activity that started the request</param>
        /// <param name="requestCode">The request code used to start the activity</param>
        /// <param name="resultCode">The result code returned by the child activity</param>
        /// <param name="data">The result data</param>
        void Handle(Activity activity, int requestCode, Result resultCode, Intent data);
    }
}
