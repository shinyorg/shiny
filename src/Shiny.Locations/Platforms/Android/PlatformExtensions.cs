using System;
using System.Threading.Tasks;
using Android.Gms.Location;
using Android.Gms.Tasks;
using Android.Locations;
using LocationRequest = Android.Gms.Location.LocationRequest;
using Task = System.Threading.Tasks.Task;

namespace Shiny.Locations;


public static class PlatformExtensions
{
    public static GpsReading FromNative(this Location location) => new GpsReading(
        new Position(location.Latitude, location.Longitude),
        location.Accuracy,
        DateTimeOffset.FromUnixTimeMilliseconds(location.Time),
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
        var priority = request.Accuracy switch
        {
            GpsAccuracy.Lowest => Priority.PriorityPassive,
            GpsAccuracy.Low => Priority.PriorityLowPower,
            GpsAccuracy.Normal => Priority.PriorityBalancedPowerAccuracy,
            GpsAccuracy.High => Priority.PriorityHighAccuracy,
            GpsAccuracy.Highest => Priority.PriorityHighAccuracy
        };

        var builder = new LocationRequest.Builder(priority, 1000)
            .SetGranularity(Granularity.GranularityPermissionLevel);

        if (request.DistanceFilterMeters > 0)
            builder = builder.SetMinUpdateDistanceMeters((float)request.DistanceFilterMeters);
        
        if (request is AndroidGpsRequest android)
        {
            builder = builder
                .SetWaitForAccurateLocation(android.WaitForAccurateLocation);

            if (android.IntervalMillis > 0)
                builder = builder.SetDurationMillis(android.IntervalMillis);
        }

        var nativeRequest = builder.Build();
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