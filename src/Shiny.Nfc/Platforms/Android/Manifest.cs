using Android;
using Android.App;

[assembly: UsesPermission(Manifest.Permission.Nfc)]
[assembly: UsesFeature("android.hardware.nfc", Required = false)]