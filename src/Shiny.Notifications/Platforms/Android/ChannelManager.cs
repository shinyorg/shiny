using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Media;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager, IShinyComponentStartup
{
    readonly IRepository repository;
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    readonly Android.App.NotificationManager nativeManager;


    public ChannelManager(
        IRepository repository,
        ILogger<ChannelManager> logger,
        AndroidPlatform platform
    )
    {
        this.repository = repository;
        this.logger = logger;
        this.platform = platform;
        this.nativeManager = platform.GetSystemService<Android.App.NotificationManager>(Context.NotificationService);
    }


    public void ComponentStart()
    {
        this.logger.LogInformation("Initializing channel manager");
        try
        {
            this.Add(Channel.Default);
            this.logger.LogInformation("Default notification channel created");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to create default channel");
        }
    }


    public void Add(Channel channel)
    {
        channel.AssertValid();
        var android = channel.TryToNative<AndroidChannel>();

        var native = new NotificationChannel(
            channel.Identifier,
            channel.Description ?? channel.Identifier,
            channel.Importance switch
            {
                ChannelImportance.Critical => NotificationImportance.Max,
                ChannelImportance.High => NotificationImportance.High,
                ChannelImportance.Normal => NotificationImportance.Default,
                ChannelImportance.Low => NotificationImportance.Low,
                _ => throw new ArgumentException("Invalid channel importance type")
            }
        );
        var attrBuilder = new AudioAttributes.Builder();
        if (channel.Importance == ChannelImportance.Critical)
        {
            attrBuilder
                .SetUsage(AudioUsageKind.Alarm)
                .SetFlags(AudioFlags.AudibilityEnforced);

            native.SetBypassDnd(true);
        }
        else
        {
            attrBuilder.SetUsage(AudioUsageKind.Notification);
        }

        switch (channel.Sound)
        {
            case ChannelSound.High:
                native.SetSound(Android.Provider.Settings.System.DefaultAlarmAlertUri, attrBuilder.Build());
                break;

            case ChannelSound.Default:
                native.SetSound(Android.Provider.Settings.System.DefaultNotificationUri, attrBuilder.Build());
                break;

            case ChannelSound.Custom:
                var uri = this.GetSoundResourceUri(channel.CustomSoundPath!);
                native.SetSound(uri, attrBuilder.Build());
                break;

            case ChannelSound.None:
                native.SetSound(null, null);
                break;
        }
        if (android.Description != null)
            native.Description = android.Description;

        if (android.AllowBubbles != null && OperatingSystemShim.IsAndroidVersionAtLeast(26))
            native.SetAllowBubbles(android.AllowBubbles.Value!);

        if (android.ShowBadge != null)
            native.SetShowBadge(android.ShowBadge.Value);

        if (android.EnableLights != null)
            native.EnableLights(android.EnableLights.Value);

        if (android.EnableVibration != null)
            native.EnableVibration(android.EnableVibration.Value);

        if (android.LockscreenVisibility != null)
            native.LockscreenVisibility = android.LockscreenVisibility.Value;

#if !MONOANDROID
        if (android.Blockable != null && OperatingSystemShim.IsAndroidVersionAtLeast(33))
            native.Blockable = android.Blockable.Value;
#endif

        if (android.BypassDnd != null)
            native.SetBypassDnd(android.BypassDnd.Value);

        this.nativeManager.CreateNotificationChannel(native);
        this.repository.Set(android);
    }


    public void Clear()
    {
        var channels = this.GetAll();
        foreach (var channel in channels)
            this.nativeManager.DeleteNotificationChannel(channel.Identifier);

        this.repository.Clear<AndroidChannel>();
        this.Add(Channel.Default);
    }


    public Channel? Get(string channelId) => this.repository.Get<AndroidChannel>(channelId);
    public IList<Channel> GetAll() => this.repository.GetList<AndroidChannel>().OfType<Channel>().ToList();
    public void Remove(string channelId)
    {
        this.AssertChannelRemove(channelId);

        this.nativeManager.DeleteNotificationChannel(channelId);
        this.repository.Remove<AndroidChannel>(channelId);
    }


    // Construct a raw resource path of the form
    // "android.resource://<PKG_NAME>/raw/<RES_NAME>", e.g.
    // "android.resource://com.shiny.sample/raw/notification"
    protected Android.Net.Uri GetSoundResourceUri(string soundResourceName)
    {
        // Strip file extension and leading slash from resource name to allow users
        // to specify custom sounds like "notification.mp3" or "/raw/notification.mp3"
        if (File.Exists(soundResourceName))
            return Android.Net.Uri.Parse("file://" + soundResourceName)!;

        soundResourceName = soundResourceName.TrimStart('/').Split('.').First();
        var resourceId = this.platform.GetRawResourceIdByName(soundResourceName);
        var resources = this.platform.AppContext.Resources!;

        return new Android.Net.Uri.Builder()
            .Scheme(ContentResolver.SchemeAndroidResource)!
            .Authority(resources.GetResourcePackageName(resourceId))!
            .AppendPath(resources.GetResourceTypeName(resourceId))!
            .AppendPath(resources.GetResourceEntryName(resourceId))!
            .Build()!;
    }
}