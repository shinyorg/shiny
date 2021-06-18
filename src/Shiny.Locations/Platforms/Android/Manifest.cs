using Android.App;
using static Android.Manifest;

[assembly: UsesPermission(Permission.AccessFineLocation)]
[assembly: UsesPermission(Permission.AccessCoarseLocation)]
[assembly: UsesPermission(Permission.AccessBackgroundLocation)]
[assembly: UsesPermission(Permission.ActivityRecognition)]
[assembly: UsesPermission(Permission.ReceiveBootCompleted)]

[assembly: UsesFeature("android.hardware.location.gps", Required = false)]
[assembly: UsesFeature("android.hardware.location.network", Required = false)]