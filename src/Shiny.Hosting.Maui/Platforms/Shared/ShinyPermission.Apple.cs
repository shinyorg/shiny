#if APPLE
using System;
using System.Linq;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using MauiPermissionException = Microsoft.Maui.ApplicationModel.PermissionException;

namespace Shiny;


public abstract partial class ShinyPermission
{
    /// <summary>
    /// Checks the Info.plist entries this permission needs - call <see cref="RequireInfoPlistKey"/> for each.
    /// </summary>
    protected virtual void EnsureInfoPlist() { }


    void EnsurePlatformDeclared() => this.EnsureInfoPlist();


    bool ShouldShowPlatformRationale() => false;


    /// <summary>
    /// Throws unless at least one of the keys is in the app's Info.plist.
    /// </summary>
    protected internal static void RequireInfoPlistKey(params string[] anyOf)
    {
        if (!anyOf.Any(HasInfoPlistKey))
            throw new MauiPermissionException($"You must set `{String.Join("` or `", anyOf)}` in your Info.plist");
    }


    protected internal static bool HasInfoPlistKey(string key)
        => NSBundle.MainBundle.ObjectForInfoDictionary(key) != null;


    /// <summary>
    /// Runs on the main thread - CoreLocation, CoreBluetooth and UIKit deliver their callbacks to the run loop the
    /// object was created on, and the permission prompts must come from the main thread.
    /// </summary>
    protected internal static Task<T> OnMainThread<T>(Func<Task<T>> func)
    {
        if (NSThread.IsMain)
            return func();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatchQueue.MainQueue.DispatchAsync(async () =>
        {
            try
            {
                tcs.TrySetResult(await func());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }


    protected internal static Task<T> OnMainThread<T>(Func<T> func)
        => OnMainThread(() => Task.FromResult(func()));
}
#endif
