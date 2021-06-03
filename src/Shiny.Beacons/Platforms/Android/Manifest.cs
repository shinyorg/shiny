using Android.App;

[assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.AccessBackgroundLocation)]
[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]
[assembly: UsesPermission(Android.Manifest.Permission.BluetoothAdmin)]
[assembly: UsesPermission(Android.Manifest.Permission.BluetoothPrivileged)]

[assembly: UsesFeature("android.hardware.bluetooth")]
[assembly: UsesFeature("android.hardware.bluetooth_le")]