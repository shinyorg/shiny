using Android.Telecom;
using Microsoft.Extensions.Logging;
using Shiny.CallDirectory.Infrastructure;
using Shiny.Hosting;

namespace Shiny.CallDirectory;


/// <summary>
/// Android's hook into every incoming call while the app holds the call screening role. It looks the number up in
/// the call directory and blocks, labels or lets the call through. Merged into the app's manifest from the package.
/// </summary>
/// <remarks>
/// Android waits at most 5 seconds for a response before letting the call ring, and any failure here falls back
/// to allowing the call - a broken list must never block the user's phone.
/// </remarks>
// Fully qualified: Shiny.Extensions.DependencyInjection also ships a [Service] attribute.
[Android.App.Service(
    Name = "com.shiny.calldirectory.ShinyCallScreeningService",
    Exported = true,
    Permission = "android.permission.BIND_SCREENING_SERVICE"
)]
[Android.App.IntentFilter(["android.telecom.CallScreeningService"])]
public class ShinyCallScreeningService : CallScreeningService
{
    /// <inheritdoc />
    public override void OnScreenCall(Call.Details callDetails) => _ = this.ScreenAsync(callDetails);


    async Task ScreenAsync(Call.Details details)
    {
        var action = CallScreeningAction.Allow;
        try
        {
            // outgoing calls reach a screening app too (API 29+) - they are never ours to stop
            var incoming = !OperatingSystem.IsAndroidVersionAtLeast(29) || details.CallDirection == CallDirection.Incoming;
            if (incoming && Host.GetService<ICallDirectoryManager>() is CallDirectoryManager manager)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                action = await manager.Screen(details.GetHandle()?.SchemeSpecificPart, cts.Token).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Host.GetService<ILogger<ShinyCallScreeningService>>()?.LogError(ex, "Call screening failed - allowing the call");
            action = CallScreeningAction.Allow;
        }

        var response = new CallResponse.Builder();
        if (action == CallScreeningAction.Block)
        {
            response.SetDisallowCall(true);
            response.SetRejectCall(true);
            response.SetSkipNotification(true);
        }
        this.RespondToCall(details, response.Build()!);
    }
}
