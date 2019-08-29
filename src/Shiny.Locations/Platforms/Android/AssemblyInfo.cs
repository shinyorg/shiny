using System;
using Android.App;

[assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]
[assembly: UsesPermission("com.google.android.gms.permission.ACTIVITY_RECOGNITION")]