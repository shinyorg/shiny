using System;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Shiny.Logging;


namespace Shiny
{
    public class AppCenterLogger : ILogger
    {
        public void Write(Exception exception, params (string Key, string Value)[] parameters)
            => Crashes.TrackError(exception, parameters.ToDictionary());


        public void Write(string eventName, string description, params (string Key, string Value)[] parameters)
            => Analytics.TrackEvent($"[{eventName}] {description}", parameters.ToDictionary());
    }
}
