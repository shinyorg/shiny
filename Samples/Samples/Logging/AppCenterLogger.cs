using System;
using System.Collections.Generic;
using System.Linq;
using Shiny.Logging;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;


namespace Samples.Logging
{
    public class AppCenterLogger : ILogger
    {
        public AppCenterLogger()
        {
            AppCenter.Start(
                Constants.AppCenterTokens,
                typeof(Crashes),
                typeof(Analytics)
            );
        }


        public void Write(Exception exception, params (string Key, string Value)[] parameters)
        {
            if (!exception.Message.StartsWith("TEST"))
                Crashes.TrackError(exception, ToDictionary(parameters));
        }


        public void Write(string eventName, string details, params (string Key, string Value)[] parameters)
            => Analytics.TrackEvent(eventName, ToDictionary(parameters));


        static IDictionary<string, string> ToDictionary((string Key, string Value)[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
                return null;

            return parameters.ToDictionary(
                x => x.Key,
                x => x.Value
            );
        }
    }
}
