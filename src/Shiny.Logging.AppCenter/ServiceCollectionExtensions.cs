using System;
using System.Collections.Generic;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
        public static void UseAppCenterLogging(this IServiceCollection services,
                                               string appSecret,
                                               bool crashes = true,
                                               bool events = true)
            => UseAppCenterLogging(appSecret, crashes, events);


        static void UseAppCenterLogging(string appSecret,
                                        bool crashes,
                                        bool events)
        {
            if (!crashes && !events)
                return;

            if (appSecret.IsEmpty())
                throw new ArgumentException("AppCenter secret was not set");

            var list = new List<Type>(2);
            if (crashes)
                list.Add(typeof(Crashes));

            if (events)
                list.Add(typeof(Analytics));

            AppCenter.Start(appSecret, list.ToArray());
            Log.AddLogger(new AppCenterLogger(), crashes, events);
        }
    }
}
