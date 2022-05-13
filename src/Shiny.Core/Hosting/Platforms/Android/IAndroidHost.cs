using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using Shiny.Hosting;

namespace Shiny;


public interface IAndroidHost : IHost
{
    string AppIdentifier { get; }
    void InvokeOnMainThread(Action action);

    DirectoryInfo AppData { get; }
    DirectoryInfo Cache { get; }
    DirectoryInfo Public { get; }


    //Activity? CurrentActivity { get; }

    Application AppContext { get; }
    PackageInfo Package { get; }

    bool IsMinApiLevel(int apiLevel);
    IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
    IObservable<AccessState> RequestAccess(string androidPermission);
    AccessState GetCurrentAccessState(string androidPermission);
    
    void StartService(Type serviceType);
    void StopService(Type serviceType);
}