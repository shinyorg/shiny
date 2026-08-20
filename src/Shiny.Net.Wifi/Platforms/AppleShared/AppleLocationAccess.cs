using CoreLocation;

namespace Shiny.Net.Wifi;


/// <summary>
/// Requests the location permission that gates SSID disclosure on Apple's platforms.
/// </summary>
/// <remarks>
/// <para>Since iOS 13 and macOS 14, an app that has not been granted location access reads the
/// joined SSID back as a placeholder ("Wi-Fi" or "WLAN") rather than the real name. Nothing fails,
/// which is why this exists as an explicit step - the alternative is a caller wondering why every
/// network is called "Wi-Fi".</para>
/// <para>Requires <c>NSLocationWhenInUseUsageDescription</c> in Info.plist. Without it the prompt
/// never appears and authorization stays NotDetermined forever.</para>
/// </remarks>
class AppleLocationAccess : IDisposable
{
    CLLocationManager? manager;
    TaskCompletionSource<CLAuthorizationStatus>? pending;


    public Task<AccessState> Request(CancellationToken ct)
    {
        this.manager ??= new CLLocationManager();
        var status = this.manager.AuthorizationStatus;

        if (status != CLAuthorizationStatus.NotDetermined)
            return Task.FromResult(ToAccessState(status));

        this.pending = new TaskCompletionSource<CLAuthorizationStatus>();
        this.manager.AuthorizationChanged += this.OnAuthorizationChanged;
        this.manager.RequestWhenInUseAuthorization();

        return this.Await(ct);
    }


    async Task<AccessState> Await(CancellationToken ct)
    {
        var tcs = this.pending!;
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

        try
        {
            var status = await tcs.Task.ConfigureAwait(false);
            return ToAccessState(status);
        }
        finally
        {
            if (this.manager != null)
                this.manager.AuthorizationChanged -= this.OnAuthorizationChanged;

            this.pending = null;
        }
    }


    void OnAuthorizationChanged(object? sender, CLAuthorizationChangedEventArgs e)
    {
        // the delegate fires once with NotDetermined before the prompt is answered
        if (e.Status != CLAuthorizationStatus.NotDetermined)
            this.pending?.TrySetResult(e.Status);
    }


    static AccessState ToAccessState(CLAuthorizationStatus status) => status switch
    {
        // Authorized and AuthorizedAlways share a value - the former is the pre-iOS 8 spelling
        CLAuthorizationStatus.AuthorizedAlways => AccessState.Available,
        CLAuthorizationStatus.AuthorizedWhenInUse => AccessState.Available,
        CLAuthorizationStatus.Denied => AccessState.Denied,
        CLAuthorizationStatus.Restricted => AccessState.Restricted,
        _ => AccessState.Unknown
    };


    public void Dispose()
    {
        if (this.manager != null)
        {
            this.manager.AuthorizationChanged -= this.OnAuthorizationChanged;
            this.manager.Dispose();
            this.manager = null;
        }
    }
}
