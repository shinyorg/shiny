using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.MediaSync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseMediaSync<TDelegate>(this IServiceCollection services)
            where TDelegate : class, IMediaSyncDelegate
            => services.UseMediaSync(typeof(TDelegate));


        public static bool UseMediaSync(this IServiceCollection services, Type? delegateType = null)
        {
#if NETSTANDARD
            return false;
#else
            services.RegisterModule(new MediaSyncModule(delegateType));
            return true;
#endif
        }
    }
}