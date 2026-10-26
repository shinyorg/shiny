using System;
using System.IO;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace Shiny.Notifications;


public static class PlatformExtensions
{
    public static int GetColorResourceId(this AndroidPlatform platform, string colorResourceName)
    {
        var colorResourceId = platform.GetColorByName(colorResourceName);
        if (colorResourceId <= 0)
            throw new ArgumentException($"Color ResourceId for {colorResourceName} not found");

        return ContextCompat.GetColor(platform.AppContext, colorResourceId);
    }


    public static int GetSmallIconResource(this AndroidPlatform platform, string? resourceName)
    {
        if (resourceName.IsEmpty())
        {
            var id = platform.GetResourceIdByName("notification");
            if (id > 0)
                return id;

            id = platform.AppContext.ApplicationInfo!.Icon;
            if (id > 0)
                return id;

            throw new InvalidOperationException("Default notification icon not found - you need to create an android resource named notification or set the application icon");
        }
        var smallIconResourceId = platform.GetResourceIdByName(resourceName!);
        if (smallIconResourceId <= 0)
            throw new ArgumentException($"Icon ResourceId for {resourceName} not found");

        return smallIconResourceId;
    }


    public static void TrySetLargeIconResource(this AndroidPlatform platform, string? largeIconResourceName, NotificationCompat.Builder builder)
    {
        if (!largeIconResourceName.IsEmpty())
        {
            var iconId = platform.GetResourceIdByName(largeIconResourceName!);
            if (iconId > 0)
                builder.SetLargeIcon(BitmapFactory.DecodeResource(platform.AppContext.Resources, iconId));
        }
    }


    public static void TrySetImage(this NotificationCompat.Builder builder, string localAttachmentFilePath)
    {
        var bitmap = DecodeNotificationImage(localAttachmentFilePath);
        if (bitmap != null)
            builder.SetStyle(new NotificationCompat.BigPictureStyle().BigPicture(bitmap));
    }


    /// <summary>
    /// Decodes an image file scaled down to at most <paramref name="maxDimension"/> pixels on its longest side - a full
    /// size photo decoded as-is can run to tens of MB, and a notification never shows more than a screen width.
    /// </summary>
    public static Bitmap? DecodeNotificationImage(string path, int maxDimension = 1440)
    {
        var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeFile(path, bounds);
        if (bounds.OutWidth <= 0 || bounds.OutHeight <= 0)
            return null;

        var sample = 1;
        while (Math.Max(bounds.OutWidth, bounds.OutHeight) / (sample * 2) >= maxDimension)
            sample *= 2;

        return BitmapFactory.DecodeFile(path, new BitmapFactory.Options { InSampleSize = sample });
    }
}
