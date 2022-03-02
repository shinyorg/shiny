using Android.App;
using static Android.Manifest;

[assembly: UsesPermission(Permission.ReceiveBootCompleted)]
[assembly: UsesPermission(Permission.Bluetooth)]
[assembly: UsesPermission(Permission.BluetoothAdmin)]
[assembly: UsesPermission(Permission.BluetoothPrivileged)]
[assembly: UsesPermission(Permission.BluetoothScan)]
[assembly: UsesPermission(Permission.BluetoothConnect)]
[assembly: UsesPermission(Permission.AccessFineLocation, MaxSdkVersion = 31)]
[assembly: UsesPermission(Permission.AccessCoarseLocation, MaxSdkVersion = 31)]

[assembly: UsesFeature("android.hardware.bluetooth", Required = false)]
[assembly: UsesFeature("android.hardware.bluetooth_le", Required = false)]