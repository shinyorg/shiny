using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Push.Blazor;


public class PushManager : NotifyPropertyChanged, IPushManager, IAsyncDisposable
{
    readonly IJSRuntime jsRuntime;
    readonly WebPushOptions options;
    readonly IServiceProvider services;
    readonly ILogger logger;
    IJSObjectReference? module;
    DotNetObjectReference<PushManager>? selfRef;


    public PushManager(
        IJSRuntime jsRuntime,
        WebPushOptions options,
        IServiceProvider services,
        ILogger<PushManager> logger
    )
    {
        this.jsRuntime = jsRuntime;
        this.options = options;
        this.services = services;
        this.logger = logger;
    }


    public IPushTagSupport? Tags => null;


    string? regToken;
    public string? RegistrationToken
    {
        get => this.regToken;
        set => this.Set(ref this.regToken, value);
    }


    string? nativeToken;
    public string? NativeRegistrationToken
    {
        get => this.nativeToken;
        set => this.Set(ref this.nativeToken, value);
    }


    async Task<IJSObjectReference> GetModule()
    {
        this.module ??= await this.jsRuntime
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
                this.options.VapidPublicKey,
                this.options.ServiceWorkerPath,
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

            await this.services
                .RunDelegates<IPushDelegate>(
                    x => x.OnNewToken(result.Subscription),
                    this.logger
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

        await this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnUnRegistered(oldToken),
                this.logger
            )
            .ConfigureAwait(false);
    }


    [JSInvokable]
    public Task OnPushReceived(Dictionary<string, string>? data, string? title, string? body)
    {
        var notification = (title != null || body != null) ? new Notification(title, body) : null;
        var push = new PushNotification(data ?? new Dictionary<string, string>(), notification);
        return this.services.RunDelegates<IPushDelegate>(x => x.OnReceived(push), this.logger);
    }


    [JSInvokable]
    public Task OnNotificationClicked(Dictionary<string, string>? data, string? title, string? body)
    {
        var notification = (title != null || body != null) ? new Notification(title, body) : null;
        var push = new PushNotification(data ?? new Dictionary<string, string>(), notification);
        return this.services.RunDelegates<IPushDelegate>(x => x.OnEntry(push), this.logger);
    }


    [JSInvokable]
    public async Task OnSubscriptionChanged(string newSubscription, string newEndpoint)
    {
        this.NativeRegistrationToken = newEndpoint;
        this.RegistrationToken = newSubscription;

        await this.services
            .RunDelegates<IPushDelegate>(
                x => x.OnNewToken(newSubscription),
                this.logger
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
