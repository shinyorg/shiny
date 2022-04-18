using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Notifications
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddChannelManager(this IServiceCollection services)
        {
#if XAMARINIOS || __ANDROID__
            services.TryAddSingleton<IChannelManager, ChannelManager>();
#endif
            return services;
        }
    }
}
