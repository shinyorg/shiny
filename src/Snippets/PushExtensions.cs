using System.Threading.Tasks;
using Shiny;
using Shiny.Push;

public class Extensions
{
    public async Task Method()
    {
        var push = ShinyHost.Resolve<IPushManager>();

        var supported = push.IsTagsSupport();

        // tries to set a params list of tags if available
        await push.TrySetTags("tag1", "tag2");

        // gets a list of currently set tags
        var tags = push.TryGetTags();

        // requests permission from the user and sets tags if available
        var permissionResult = await push.TryRequestAccessWithTags("tag1", "tag2");
    }
}