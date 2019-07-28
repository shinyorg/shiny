using System;
using Microsoft.Extensions.DependencyInjection;
using Plugin.CurrentActivity;


namespace Shiny
{
    public static class Extensions
    {
        internal static ActivityState FromPlugin(this ActivityEvent @event)
            => (ActivityState)Enum.Parse(typeof(ActivityState), @event.ToString(), true);


        /// <summary>
        ///
        /// </summary>
        public static void UseCurrentActivityPlugin(this IServiceCollection services)
            => services.AddSingleton<ITopActivity, TopActivityImpl>();
    }
}
