using System;
using Samples.Models;
using Shiny;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Samples.Logging
{
    public class DbLogger : ILogger
    {
        public void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            // ctor DI is not available in loggers!
            var conn = ShinyHost.Resolve<SampleSqliteConnection>();
            var serializer = ShinyHost.Resolve<ISerializer>();
            conn.GetConnection().Insert(new ErrorLog
            {
                Description = exception.ToString(),
                Parameters = serializer.Serialize(parameters),
                Timestamp = DateTime.Now
            });
        }


        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
            //var conn = ShinyHost.Resolve<SampleSqliteConnection>();
            //var serializer = ShinyHost.Resolve<ISerializer>();
            //conn.GetConnection().Insert(new ErrorLog
            //{
            //    Description = exception.ToString(),
            //    Parameters = serializer.Serialize(parameters),
            //    Timestamp = DateTime.Now
            //});
        }
    }
}
