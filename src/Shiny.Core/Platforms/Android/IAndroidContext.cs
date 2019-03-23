using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using NativePerm = Android.Content.PM.Permission;


namespace Shiny
{
    public interface IAndroidContext
    {
        Application AppContext { get; }
        Activity CurrentActivity { get; }
        PackageInfo Package { get; }

        event EventHandler<PermissionRequestResult> PermissionResult;
        void FirePermission(int requestCode, string[] permissions, NativePerm[] grantResult);

        AccessState GetCurrentAccessState(string androidPermission);
        T GetIntentValue<T>(string intentAction, Func<Intent, T> transform);
        bool IsInManifest(string androidPermission, bool assert);
        IObservable<AccessState> RequestAccess(string androidPermission);
        void StartService(Type serviceType);
        void StopService(Type serviceType);
        IObservable<ActivityChanged> WhenActivityStatusChanged();
        IObservable<Intent> WhenIntentReceived(string intentAction);
    }
}