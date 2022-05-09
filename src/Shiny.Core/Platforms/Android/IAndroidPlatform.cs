using System;
using Android.App;
using Android.Content.PM;

namespace Shiny;


public interface IAndroidPlatform : IPlatform
{
    Application AppContext { get; }
    Activity? CurrentActivity { get; }
    PackageInfo Package { get; }

    bool IsMinApiLevel(int apiLevel);
    IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
    IObservable<AccessState> RequestAccess(string androidPermission);

    AccessState GetCurrentAccessState(string androidPermission);
    
    void StartService(Type serviceType);
    void StopService(Type serviceType);
}