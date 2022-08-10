#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;

namespace Shiny;


public static class MotionActivityServiceCollectionExtensions
{
    /// <summary>
    /// Add motion activity recognition
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
     public static IServiceCollection AddMotionActivity(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<AndroidSqliteDatabase>();
#endif
        return services.AddShinyService<MotionActivityManagerImpl>();
    }

}
#endif