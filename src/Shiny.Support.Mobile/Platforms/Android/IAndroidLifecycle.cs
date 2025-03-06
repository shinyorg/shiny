using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Shiny.Hosting;


public interface IAndroidLifecycle
{
    public interface IApplicationLifecycle
    {
        void OnForeground();
        void OnBackground();
    }

    public interface IOnActivityOnCreate
    {
        void ActivityOnCreate(Activity activity, Bundle? savedInstanceState);
    }

    public interface IOnActivityRequestPermissionsResult
    {
        void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults);
    }

    public interface IOnActivityNewIntent
    {
        void Handle(Activity activity, Intent intent);
    }

    public interface IOnActivityResult
    {
        void Handle(Activity activity, int requestCode, Result resultCode, Intent data);
    }
}
