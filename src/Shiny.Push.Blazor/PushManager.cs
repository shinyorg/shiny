using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Push.Blazor;


public class PushManager(
    IJSRuntime jsRuntime,
    WebPushOptions options,
    IServiceProvider services,
    ILogger<PushManager> logger
) : NotifyPropertyChanged, IPushManager, IAsyncDisposable
{
    IJSObjectReference? module;
    DotNetObjectReference<PushManager>? selfRef;

    public IPushTagSupport? Tags => null;


    public string? RegistrationToken
    {
        get;
        set => this.Set(ref field, value);
    }


    public string? NativeRegistrationToken
    {
        get;
        set => this.Set(ref field, value);
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await jsRuntime
            .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Push.Blazor/push.js")
            .ConfigureAwait(false);

        return this.module;
    }


    public async Task<AccessState> GetCurrentAccess()
    {
        var mod = await this.GetModule().ConfigureAwait(false);
        var state = await mod.InvokeAsync<string>("getPermission").ConfigureAwait(false);
        return MapPermission(state);
    }


    public async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        var mod = await this.GetModule().ConfigureAwait(false);
        this.selfRef ??= DotNetObjectReference.Create(this);

        var result = await mod
            .InvokeAsync<SubscribeResult?>(
                "subscribe",
                cancelToken,
                options.VapidPublicKey,
                options.ServiceWorkerPath,
                this.selfRef
            )
            .ConfigureAwait(false);

        if (result == null)
            return new PushAccessState(AccessState.NotSupported, null);

        var access = MapPermission(result.Permission);
        if (access != AccessState.Available || result.Subscription == null)
            return new PushAccessState(access, null);

        if (this.RegistrationToken != result.Subscription)
        {
            this.NativeRegistrationToken = result.Endpoint;
            this.RegistrationToken = result.Subscription;

            await services
                .RunDelegates<IPushDelegate>(
                    x => x.OnNewToken(result.Subscription),
                    logger
                )
                .ConfigureAwait(false);
        }

        return new PushAccessState(AccessState.Available, this.RegistrationToken);
    }


    public async Task UnRegister()
    {
        if (this.RegistrationToken == null)
            return;

        var oldToken = this.RegistrationToken;
        var mod = await this.GetModule().ConfigureAwait(false);
        await mod.InvokeVoidAsync("unsubscribe").ConfigureAwait(false);

        this.RegistrationToken = null;
        this.NativeRegistrationToken = null;

        await services
            .RunDelegates<IPushDelegate>(
                x => x.OnUnRegistered(oldToken),
                logger
            )
            .ConfigureAwait(false);
    }


    [JSInvokable]
    public Task OnPushReceived(Dictionary<string, string>? data, string? title, string? body)
    {
        var notification = (title != null || body != null) ? new Notification(title, body) : null;
        var push = new PushNotification(data ?? new Dictionary<string, string>(), notification);
        return services.RunDelegates<IPushDelegate>(x => x.OnReceived(push), logger);
    }


    [JSInvokable]
    public Task OnNotificationClicked(Dictionary<string, string>? data, string? title, string? body)
    {
        var notification = (title != null || body != null) ? new Notification(title, body) : null;
        var push = new PushNotification(data ?? new Dictionary<string, string>(), notification);
        return services.RunDelegates<IPushDelegate>(x => x.OnEntry(push), logger);
    }


    [JSInvokable]
    public async Task OnSubscriptionChanged(string newSubscription, string newEndpoint)
    {
        this.NativeRegistrationToken = newEndpoint;
        this.RegistrationToken = newSubscription;

        await services
            .RunDelegates<IPushDelegate>(
                x => x.OnNewToken(newSubscription),
                logger
            )
            .ConfigureAwait(false);
    }


    static AccessState MapPermission(string state) => state switch
    {
        "granted" => AccessState.Available,
        "denied" => AccessState.Denied,
        "default" => AccessState.Unknown,
        "unsupported" => AccessState.NotSupported,
        _ => AccessState.Unknown
    };


    public async ValueTask DisposeAsync()
    {
        this.selfRef?.Dispose();
        this.selfRef = null;

        if (this.module != null)
        {
            await this.module.DisposeAsync().ConfigureAwait(false);
            this.module = null;
        }
    }


    internal record SubscribeResult(string Permission, string? Subscription, string? Endpoint);
}
