using Foundation;
using Shiny.Hosting;

namespace Shiny;


public record RegisterHandleEventsForBackgroundUrl(string SessionUrl);
public record RegisterContinueActivity(NSUserActivity Activity);


public static class IosLifecycleBuilderExtensions
{
    public static ILifecycleBuilder OnFinishedLaunching(this ILifecycleBuilder builder)
    { 
        return builder;
    }
}