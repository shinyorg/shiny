using System;
using System.IO;
#if MONOANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
#endif


namespace Shiny
{
    public enum PlatformState
    {
        Foreground,
        Background
    }


    public interface IPlatform
    {
        string Name { get; }

        PlatformState Status { get; }
        DirectoryInfo AppData { get; }
        DirectoryInfo Cache { get; }
        DirectoryInfo Public { get; }

        string AppIdentifier { get; }
        string AppVersion { get; }
        string AppBuild { get; }

        string MachineName { get; }
        string OperatingSystem { get; }
        string OperatingSystemVersion { get; }
        string Manufacturer { get; }
        string Model { get; }

        void InvokeOnMainThread(Action action);
        IObservable<PlatformState> WhenStateChanged();


        #if MONOANDROID
        Application AppContext { get; }
        Activity CurrentActivity { get; }
        PackageInfo Package { get; }
        IObservable<ActivityChanged> WhenActivityChanged();
        IObservable<(bool NewIntent, Intent Intent)> WhenIntentReceived();
        void OnNewIntent(Intent? intent);
        void OnActivityResult(int requestCode, Result resultCode, Intent data);

        bool IsMinApiLevel(int apiLevel);
        IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
        IObservable<AccessState> RequestAccess(string androidPermission);

        AccessState GetCurrentAccessState(string androidPermission);
        void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult);
        void StartService(Type serviceType);
        void StopService(Type serviceType);
        IObservable<ActivityChanged> WhenActivityStatusChanged();
        IObservable<(Result result, Intent data)> RequestActivityResult(Action<int, Activity> request);
        #endif
    }
}
