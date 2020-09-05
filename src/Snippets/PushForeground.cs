using System.Reactive;
using Shiny;
using Shiny.Push;

public class PushForeground
{
    public void YourMethod()
    {
        var push = ShinyHost.Resolve<IPushManager>(); // assign through DI, static, or ShinyHost.Resolve
        push.WhenReceived().Subscribe(data =>
        {
            var newData = data["newdata"] == "true";

        });
    }
}
