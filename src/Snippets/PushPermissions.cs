using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.Push;

public class PushRegistration
{
    public async Task CheckPermission()
    {
        var push = ShinyHost.Resolve<IPushManager>();
        var result = await push.RequestAccess();
        if (result.Status == AccessState.Available)
        {
            // good to go

            // you should send this to your server with a userId attached if you want to do custom work
            var value = result.RegistrationToken;
        }
    }
}
