namespace Shiny;

public record AndroidPermission(
    string Permission,
    int? MinSdkVersion,
    int? MaxSdkVersion
);


/// <summary>
/// This is to support Xamarin/out of band releases of android permissions
/// </summary>
public static class AndroidPermissions
{
    //Manifest.Permission.PostNotifications
    //public const string PostNotifications = "android.permission.POST_NOTIFICATIONS";
    public const string ForegroundServiceLocation = "android.permission.FOREGROUND_SERVICE_LOCATION";
}