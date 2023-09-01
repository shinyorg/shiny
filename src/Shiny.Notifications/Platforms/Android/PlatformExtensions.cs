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
        using var stream = File.OpenRead(localAttachmentFilePath);
        
        var bitmap = BitmapFactory.DecodeStream(stream);
        builder.SetStyle(new NotificationCompat.BigPictureStyle().BigPicture(bitmap));
    }
}
