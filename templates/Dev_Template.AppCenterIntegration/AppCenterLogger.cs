using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Shiny.Logging;


namespace $safeprojectname$
{
    public class AppCenterLogger : ILogger
    {
        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
              => Analytics.TrackEvent(eventName, ToDictionary(parameters));


        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => Crashes.TrackError(exception, ToDictionary(parameters));


        static IDictionary<string, string> ToDictionary((string Key, string Value)[] parameters)
        {
            if (parameters == null)
                return null;

            return parameters.ToDictionary(
                x => x.Key,
                x => x.Value
            );
        }
    }
}
