using System;
using Samples.Models;
using Shiny;
using Shiny.Logging;


namespace Samples.Loggers
{
    public class DbLogger : ILogger
    {
        public void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            // ctor DI is not available in loggers!
            ShinyHost.Resolve<SampleSqliteConnection>().GetConnection().Insert(new ErrorLog
            {
                Description = exception.ToString(),
                Timestamp = DateTime.Now
            });
        }


        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
        {
        }
    }
}
