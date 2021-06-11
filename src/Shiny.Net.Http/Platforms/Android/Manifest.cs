using Android.App;
using static Android.Manifest;

[assembly: UsesPermission(Permission.WriteExternalStorage)]
[assembly: UsesPermission(Permission.ReadExternalStorage)]
[assembly: UsesPermission(Permission.ReceiveBootCompleted)]