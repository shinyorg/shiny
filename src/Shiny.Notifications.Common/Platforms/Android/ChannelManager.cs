using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Notifications;


public class ChannelManager : IChannelManager, IShinyStartupTask
{
    readonly IRepository<Channel> repository;
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    readonly NotificationManager nativeManager;


    public ChannelManager(
        IRepository<Channel> repository,
        ILogger<ChannelManager> logger,
        AndroidPlatform platform
    )
    {
        this.repository = repository;
        this.logger = logger;
        this.platform = platform;
        this.nativeManager = platform.GetSystemService<NotificationManager>(Context.NotificationService);
    }


    public void Start()
    {
        this.logger.LogInformation("Initializing channel manager");

        this.Add(Channel.Default).ContinueWith(x =>
        {
            if (x.IsFaulted)
            {
                this.logger.LogError("Failed to create default channel", x.Exception);
            }
            else
            {
                this.logger.LogDebug("Channel manager initialized successfully");
            }
        });
    }


    public async Task Add(Channel channel)
    {
        channel.AssertValid();

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

        Android.Net.Uri? uri = null;
        if (!channel.CustomSoundPath.IsEmpty())
            uri = this.GetSoundResourceUri(channel.CustomSoundPath!);

        switch (channel.Importance)
        {
            case ChannelImportance.Critical:
                attrBuilder
                    .SetUsage(AudioUsageKind.Alarm)
                    .SetFlags(AudioFlags.AudibilityEnforced);

                uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                // if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                native.SetBypassDnd(true);
                break;

            case ChannelImportance.High:
                uri ??= Android.Provider.Settings.System.DefaultAlarmAlertUri;
                break;

            case ChannelImportance.Normal:
                uri ??= Android.Provider.Settings.System.DefaultNotificationUri;
                break;

            case ChannelImportance.Low:
                break;
        }
        if (uri != null)
            native.SetSound(uri, attrBuilder.Build());

        this.nativeManager.CreateNotificationChannel(native);
        await this.repository.Set(channel).ConfigureAwait(false);
    }


    public async Task Clear()
    {
        var channels = await this.GetAll().ConfigureAwait(false);
        foreach (var channel in channels)
            this.nativeManager.DeleteNotificationChannel(channel.Identifier);

        await this.repository
            .Clear()
            .ConfigureAwait(false);

        await this.Add(Channel.Default).ConfigureAwait(false);
    }


    public Task<Channel?> Get(string channelId) => this.repository.Get(channelId);
    public Task<IList<Channel>> GetAll() => this.repository.GetList();
    public Task Remove(string channelId)
    {
        this.AssertChannelRemove(channelId);

        this.nativeManager.DeleteNotificationChannel(channelId);
        return this.repository.Remove(channelId);
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