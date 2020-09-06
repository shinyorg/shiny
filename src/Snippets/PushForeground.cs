using System.Reactive.Linq;
using Shiny;
using Shiny.Push;

public class PushForeground
{
    public void YourMethod()
    {
        var push = ShinyHost.Resolve<IPushManager>(); // assign through DI, static, or ShinyHost.Resolve
        var disp = push
            .WhenReceived()
            .Where(x => x["newdata"] == "true")
            .SubscribeAsync(async data =>
            {
                // make you HTTP call here
            });
    }
}
