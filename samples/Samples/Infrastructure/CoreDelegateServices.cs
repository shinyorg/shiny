using System;


namespace Samples.Infrastructure
{
    public class CoreDelegateServices
    {
        public CoreDelegateServices(SampleSqliteConnection conn,
                                    AppNotifications notifications)
        {
            this.Connection = conn;
            this.Notifications = notifications;
        }


        public SampleSqliteConnection Connection { get; }
        public AppNotifications Notifications { get; }
    }
}
