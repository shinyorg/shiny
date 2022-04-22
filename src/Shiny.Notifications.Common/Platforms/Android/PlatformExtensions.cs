using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;


namespace Shiny.Notifications
{
    public static class PlatformExtensions
    {
        // Construct a raw resource path of the form
        // "android.resource://<PKG_NAME>/raw/<RES_NAME>", e.g.
        // "android.resource://com.shiny.sample/raw/notification"
        public static Android.Net.Uri GetSoundResourceUri(this IPlatform platform, string soundResourceName)
        {
            // Strip file extension and leading slash from resource name to allow users
            // to specify custom sounds like "notification.mp3" or "/raw/notification.mp3"
            if (File.Exists(soundResourceName))
                return Android.Net.Uri.Parse("file://" + soundResourceName)!;

            soundResourceName = soundResourceName.TrimStart('/').Split('.').First();
            var resourceId = platform.GetRawResourceIdByName(soundResourceName);
            var resources = platform.AppContext.Resources!;
            return new Android.Net.Uri.Builder()
                .Scheme(ContentResolver.SchemeAndroidResource)!
                .Authority(resources.GetResourcePackageName(resourceId))!
                .AppendPath(resources.GetResourceTypeName(resourceId))!
                .AppendPath(resources.GetResourceEntryName(resourceId))!
                .Build()!;
        }


        public static int GetColorResourceId(this IPlatform platform, string colorResourceName)
        {
            var colorResourceId = platform.GetColorByName(colorResourceName);
            if (colorResourceId <= 0)
                throw new ArgumentException($"Color ResourceId for {colorResourceName} not found");

            return ContextCompat.GetColor(platform.AppContext, colorResourceId);
        }


        public static int GetSmallIconResource(this IPlatform platform, string? resourceName)
        {
            if (resourceName.IsEmpty())
            {
                var id = platform.GetResourceIdByName("notification");
                if (id > 0)
                    return id;

                return platform.AppContext.ApplicationInfo!.Icon;
            }
            var smallIconResourceId = platform.GetResourceIdByName(resourceName!);
            if (smallIconResourceId <= 0)
                throw new ArgumentException($"Icon ResourceId for {resourceName} not found");

            return smallIconResourceId;
        }


        public static void TrySetLargeIconResource(this IPlatform platform, string? largeIconResourceName, NotificationCompat.Builder builder)
        {
            if (!largeIconResourceName.IsEmpty())
            {
                var iconId = platform.GetResourceIdByName(largeIconResourceName!);
                if (iconId > 0)
                    builder.SetLargeIcon(BitmapFactory.DecodeResource(platform.AppContext.Resources, iconId));
            }
        }


        static readonly HttpClient httpClient = new HttpClient();
        public static async Task TrySetImage(this IPlatform platform, string? imageUri, NotificationCompat.Builder builder)
        {
            if (imageUri.IsEmpty())
                return;

            var stream = await httpClient.GetStreamAsync(imageUri).ConfigureAwait(false);
            var bitmap = await BitmapFactory.DecodeStreamAsync(stream).ConfigureAwait(false);
            builder.SetStyle(new NotificationCompat.BigPictureStyle().BigPicture(bitmap));
        }
    }
}
