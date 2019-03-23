using System;
using System.Collections.Generic;
using System.Linq;
using Shiny.Logging;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;


namespace Samples
{
    public class AppCenterLogger : ILogger
    {
        public AppCenterLogger()
        {
            AppCenter.Start(
                "ios=a146d108-e673-4429-bd80-7ca726866b1c;android=08bcf24f-455b-46c1-9e13-c0fbfc7ae996;uwp=285b4595-6d44-4078-8888-21071f668087",
                typeof(Crashes),
                typeof(Analytics)
            );
        }


        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => Crashes.TrackError(exception, ToDictionary(parameters));


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
