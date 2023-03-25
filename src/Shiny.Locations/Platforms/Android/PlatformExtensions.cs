using System;
using System.Threading.Tasks;

using Android.Gms.Location;
using Android.Gms.Tasks;
using Android.Locations;
using LocationRequest = Android.Gms.Location.LocationRequest;
using Task = System.Threading.Tasks.Task;

namespace Shiny.Locations;


static class PlatformExtensions
{
    public static GpsReading FromNative(this Location location) => new GpsReading(
        new Position(location.Latitude, location.Longitude),
        location.Accuracy,
        DateTimeOffset.FromUnixTimeMilliseconds(location.Time).UtcDateTime,
        location.Bearing,
        location.BearingAccuracyDegrees,
        location.Altitude,
        location.Speed,
        location.SpeedAccuracyMetersPerSecond
    );


    public static Task ToTask(this Android.Gms.Tasks.Task androidTask)
    {
        var src = new AndroidTaskListener();
        androidTask.AddOnCanceledListener(src);
        androidTask.AddOnFailureListener(src);
        androidTask.AddOnSuccessListener(src);
        return src.Task;
    }


    public static LocationRequest ToNative(this GpsRequest request)
    {
        var nativeRequest = LocationRequest.Create();

        switch (request.Accuracy)
        {
            case GpsAccuracy.Lowest:
                nativeRequest
                    .SetPriority(Priority.PriorityLowPower)
                    .SetInterval(1000 * 120) // 2 mins
                    .SetSmallestDisplacement(3000);
                break;

            case GpsAccuracy.Low:
                nativeRequest
                    .SetPriority(Priority.PriorityLowPower)
                    .SetInterval(1000 * 60) // 1 min
                    .SetSmallestDisplacement(1000);
                break;

            case GpsAccuracy.Normal:
                nativeRequest
                    .SetPriority(Priority.PriorityBalancedPowerAccuracy)
                    .SetInterval(1000 * 30) // 30 seconds
                    .SetSmallestDisplacement(100);
                break;

            case GpsAccuracy.High:
                nativeRequest
                    .SetPriority(Priority.PriorityHighAccuracy)
                    .SetInterval(1000 * 10) // 10 seconds
                    .SetSmallestDisplacement(10);
                break;

            case GpsAccuracy.Highest:
                nativeRequest
                    .SetPriority(Priority.PriorityHighAccuracy)
                    .SetInterval(1000) // every second
                    .SetSmallestDisplacement(1);

                break;
        }

        return nativeRequest;
    }
}


public class AndroidTaskListener : Java.Lang.Object, IOnSuccessListener, IOnFailureListener, IOnCanceledListener
{
    readonly TaskCompletionSource<object> compSource = new();

    public Task Task => this.compSource.Task;
    public void OnCanceled() => this.compSource.SetCanceled();
    public void OnFailure(Java.Lang.Exception e) => this.compSource.SetException(e);
    public void OnSuccess(Java.Lang.Object result) => this.compSource.SetResult(null!);
}