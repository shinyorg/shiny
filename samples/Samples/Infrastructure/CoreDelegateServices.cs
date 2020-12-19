using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Shiny;
using Shiny.Notifications;
using Samples.Settings;


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
