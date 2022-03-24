using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Media;


namespace Shiny.Notifications
{
    public partial class NotificationManager
    {
        public Task<Channel?> GetChannel(string identifier)
            => this.core.Repository.GetChannel(identifier);


        public Task AddChannel(Channel channel)
        {
            channel.AssertValid();
            this.CreateNativeChannel(channel);

            return this.core.Repository.SetChannel(channel);
        }


        public Task RemoveChannel(string channelId)
        {
            this.manager.NativeManager.DeleteNotificationChannel(channelId);
            return this.core.Repository.RemoveChannel(channelId);
        }


        public async Task ClearChannels()
        {
            var channels = await this.core
                .Repository
                .GetChannels()
                .ConfigureAwait(false);

            foreach (var channel in channels)
                this.manager.NativeManager.DeleteNotificationChannel(channel.Identifier);

            await this.core
                .Repository
                .RemoveAllChannels()
                .ConfigureAwait(false);
        }


        public Task<IList<Channel>> GetChannels()
            => this.core.Repository.GetChannels();


        protected async Task<Channel> GetChannel(Notification notification)
        {
            var channel = Channel.Default;

            if (notification.Channel.IsEmpty())
            {
                if (this.manager.NativeManager.GetNotificationChannel(Channel.Default.Identifier) == null)
                    this.CreateNativeChannel(Channel.Default);
            }
            else
            {
                channel = await this.core
                    .Repository
                    .GetChannel(notification.Channel!)
                    .ConfigureAwait(false);

                if (channel == null)
                    throw new InvalidOperationException($"{notification.Channel} does not exist");
            }
            return channel;
        }


        protected virtual void CreateNativeChannel(Channel channel)
        {
            var native = new NotificationChannel(
                channel.Identifier,
                channel.Description ?? channel.Identifier,
                channel.Importance.ToNative()
            );
            var attrBuilder = new AudioAttributes.Builder();

            Android.Net.Uri? uri = null;
            if (!channel.CustomSoundPath.IsEmpty())
                uri = this.manager.GetSoundResourceUri(channel.CustomSoundPath!);

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

            this.manager.NativeManager.CreateNotificationChannel(native);
        }
    }
}
