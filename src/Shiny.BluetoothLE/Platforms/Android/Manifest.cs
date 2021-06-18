using Android.App;
using static Android.Manifest;

[assembly: UsesPermission(Permission.AccessFineLocation)]
[assembly: UsesPermission(Permission.AccessCoarseLocation)]
[assembly: UsesPermission(Permission.AccessBackgroundLocation)]
[assembly: UsesPermission(Permission.Bluetooth)]
[assembly: UsesPermission(Permission.BluetoothAdmin)]
[assembly: UsesPermission(Permission.BluetoothPrivileged)]
[assembly: UsesPermission(Permission.ReceiveBootCompleted)]

[assembly: UsesFeature("android.hardware.bluetooth", Required = false)]
[assembly: UsesFeature("android.hardware.bluetooth_le", Required = false)]