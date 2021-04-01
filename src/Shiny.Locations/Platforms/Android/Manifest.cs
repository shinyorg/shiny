using Android.App;

[assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessBackgroundLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.ActivityRecognition)]
//[assembly: UsesPermission(Android.Manifest.Permission.ReceiveBootCompleted)]

[assembly: UsesFeature("android.hardware.location.gps")]
[assembly: UsesFeature("android.hardware.location.network")]