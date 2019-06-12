using System;
using Microsoft.Extensions.DependencyInjection;
using Plugin.CurrentActivity;


namespace Shiny.Integrations.CurrentActivityPlugin
{
    public static class Extensions
    {
        internal static ActivityState FromPlugin(this ActivityEvent @event)
            => (ActivityState)Enum.Parse(typeof(ActivityState), @event.ToString(), true);


        /// <summary>
        ///
        /// </summary>
        /// <param name="autoInit">If true, Shiny will Init the activity plugin using its application context (you should set to true)</param>
        public static void UseCurrentActivityIntegration(this IServiceCollection services, bool autoInit)
            => services.AddOrReplace<ITopActivity>(new TopActivityImpl(autoInit));
    }
}
