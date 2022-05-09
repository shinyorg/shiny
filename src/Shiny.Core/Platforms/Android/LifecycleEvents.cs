namespace Shiny;

using System;
using Android.App;
using Android.Content;
using Shiny.Hosting;

// TODO: I have to add some of the lifecycle methods for android like the following: 
// TODO: need a holder for current activity
/*
FROM APP             
this.callbacks = new ActivityLifecycleCallbacks(); // current activity is here
this.app.RegisterActivityLifecycleCallbacks(this.callbacks);
ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
 * 
public class AndroidPlatform : Java.Lang.Object,
                                ILifecycleObserver,
                                IPlatform,
                                IPlatformBuilder


readonly ActivityLifecycleCallbacks callbacks;

[Lifecycle.Event.OnResume]
[Export]
public void OnResume()
{
    this.Status = PlatformState.Foreground;
    this.stateSubj.OnNext(PlatformState.Foreground);
}


[Lifecycle.Event.OnPause]
[Export]
public void OnPause()
{
    this.Status = PlatformState.Background;
    this.stateSubj.OnNext(PlatformState.Background);
}
 */

public record ActivityResult(Activity Activity, Result Result, Intent Intent);
public record ActivityPermissionResult(Activity Activity);



public static class AndroidLifecycleBuilderEvents
{
    public static ILifecycleBuilder OnActivityPermissionResult(this ILifecycleBuilder builder, Action<ActivityPermissionResult> action)
    {
        //builder.On(typeof(ActivityPermissionResult), action);

        return builder;
    }
}