using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class ChannelManager : IChannelManager, IShinyStartupTask
    {
        readonly IRepository repository;
        readonly IPlatform platform;
        readonly ILogger logger;
        readonly NotificationManager nativeManager;


        public ChannelManager(
            IRepository repository,
            ILogger<ChannelManager> logger,
            IPlatform platform
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

            if (channel.Importance == ChannelImportance.Critical)
            {
                attrBuilder
                    .SetUsage(AudioUsageKind.Alarm)
                    .SetFlags(AudioFlags.AudibilityEnforced);

                native.SetBypassDnd(true);
            }

            switch (channel.Sound)
            {
                case ChannelSound.None:
                    native.SetSound(null, null);
                    break;

                case ChannelSound.Default:
                    native.SetSound(Android.Provider.Settings.System.DefaultNotificationUri, attrBuilder.Build());
                    break;

                case ChannelSound.High:
                    native.SetSound(Android.Provider.Settings.System.DefaultAlarmAlertUri, attrBuilder.Build());
                    break;

                case ChannelSound.Custom:
                    var uri = this.platform.GetSoundResourceUri(channel.CustomSoundPath!);
                    native.SetSound(uri, attrBuilder.Build());
                    break;
            }

            this.nativeManager.CreateNotificationChannel(native);
            await this.repository.Set(channel.Identifier, channel);
        }


        public async Task Clear()
        {
            var channels = await this.GetAll().ConfigureAwait(false);
            foreach (var channel in channels)
                this.nativeManager.DeleteNotificationChannel(channel.Identifier);

            await this.repository
                .Clear<Channel>()
                .ConfigureAwait(false);

            await this.Add(Channel.Default).ConfigureAwait(false);
        }


        public Task<Channel?> Get(string channelId) => this.repository.Get<Channel>(channelId);
        public Task<IList<Channel>> GetAll() => this.repository.GetList<Channel>();
        public Task Remove(string channelId)
        {
            this.AssertChannelRemove(channelId);

            this.nativeManager.DeleteNotificationChannel(channelId);
            return this.repository.Remove<Channel>(channelId);
        }
    }
}