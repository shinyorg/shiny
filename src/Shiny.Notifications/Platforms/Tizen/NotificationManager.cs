using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Settings;
using NativeManager = Tizen.Applications.Notifications.NotificationManager;
using NativeNot = Tizen.Applications.Notifications.Notification;


namespace Shiny.Notifications
{
    //https://developer.tizen.org/development/guides/.net-application/notifications-and-content-sharing/notifications
    //https://developer.tizen.org/development/guides/.net-application/alarms
    public class NotificationManager : INotificationManager
    {
        readonly ShinyCoreServices services;
        public NotificationManager(ShinyCoreServices services)
            => this.services = services;


        public async Task Cancel(int id)
        {
            await this.repository.Remove<Notification>(id.ToString());
            var native = NativeManager.Load(id.ToString());
            if (native != null)
                NativeManager.Delete(native);
        }


        public async Task Clear()
        {
            await this.repository.Clear<Notification>();
            NativeManager.DeleteAll();
        }


        public int Badge { get; set; }

        public async Task<IEnumerable<Notification>> GetPending()
            => await this.repository.GetAll<Notification>();


        public Task<AccessState> RequestAccess() => Platform.RequestAccess("notification");


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            var native = this.Create(notification);
            //AlarmManager.CreateAlarm(notification.ScheduleDate.Value.ToLocalTime(), AlarmWeekFlag.AllDays, native);

            NativeManager.Post(native);
            await this.repository.Set(notification.Id.ToString(), notification);
        }


        readonly List<NotificationCategory> registeredCategories = new List<NotificationCategory>();
        public void RegisterCategory(NotificationCategory category) => this.registeredCategories.Add(category);


        NativeNot Create(Notification notification)
        {
            //DirectoryInfo info = Application.Current.DirectoryInfo;
            //String imagePath;
            //String sharedPath = info.SharedData;
            //imagePath = sharedPath + "imageName.png";
            var native = new NativeNot
            {
                Tag = notification.Id.ToString(),
                Title = notification.Title,
                Content = notification.Message,
                //Icon = imagePath,
                //Count = 2,
                //TimeStamp = time,
                //Property = DisableAppLaunch
            };

            //if (!Notification.CustomSoundFilePath.IsEmpty())
            //{
            //    native.Accessory = new NativeNot.AccessorySet
            //    {
            //        //CanVibrate = true,
            //        //LedOnMillisecond = 100,
            //        //LedOffMillisecond = 100,
            //        //SoundOption = AccessoryOption.Custom,
            //        //SoundPath = Path.Combine(this.fileSystem.AppData.FullName, notification.Sound)
            //        SoundPath = Notification.CustomSoundFilePath
            //    };
            //}
            return native;
        }
    }
}
