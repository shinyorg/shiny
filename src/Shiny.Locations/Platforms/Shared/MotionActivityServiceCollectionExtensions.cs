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
     public static bool AddMotionActivity(this IServiceCollection services)
    {
#if ANDROID
        services.AddSingleton<AndroidSqliteDatabase>();
#endif

#if ANDROID || IOS || MACCATALYST
        services.AddShinyService<MotionActivityManagerImpl>();
        return true;
#else
        return false;
#endif
    }

}