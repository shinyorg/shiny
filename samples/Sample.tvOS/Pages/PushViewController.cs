using Sample.tvOS.Infrastructure;
using Sample.tvOS.Services;
using Shiny.Push;

namespace Sample.tvOS.Pages;


/// <summary>
/// tvOS registers with APNs and receives silent pushes exactly as iOS does. What it does not have
/// is a notification anyone can see or tap: UNNotificationContent on tvOS carries nothing but a
/// badge count, so RequestAccess() asks for Badge alone and IPushDelegate.OnEntry never fires.
/// Treat a tvOS push as "go fetch something", never as a message to read.
/// </summary>
public class PushViewController() : ModuleViewController(
    "Shiny.Push - APNs. tvOS gets silent push and the app icon badge, and nothing else"
)
{
    protected override void OnReady()
    {
        var log = Resolve<AppLog>();
        log.Written += (_, msg) => this.Log(msg);

        this.AddAction("Register", async () =>
        {
            var push = Resolve<IPushManager>();
            this.Log("requesting access (Badge only on tvOS)...");

            var result = await push.RequestAccess();
            this.Log($"access: {result.Status}");

            if (result.RegistrationToken != null)
                this.Log($"token: {result.RegistrationToken}");
        });

        this.AddAction("Show token", async () =>
        {
            var push = Resolve<IPushManager>();
            this.Log($"current access:  {await push.GetCurrentAccess()}");
            this.Log($"registration:    {push.RegistrationToken ?? "(none)"}");
            this.Log($"native APNs:     {push.NativeRegistrationToken ?? "(none)"}");
        });

        this.AddAction("Set badge", () =>
        {
            // the only user-visible surface a tvOS notification has
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 3;
            this.Log("app icon badge set to 3 - that is the whole of tvOS notification UI");
            return Task.CompletedTask;
        });

        this.AddAction("Unregister", async () =>
        {
            await Resolve<IPushManager>().UnRegister();
            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;
            this.Log("unregistered and badge cleared");
        });

        this.Log("Push requires a real Apple TV - the simulator gets no APNs token");
    }
}
