using System.Runtime.InteropServices;

namespace Shiny.InAppPurchases;


/// <summary>
/// P/Invoke surface of the ShinyStoreKit Swift framework (native/ShinyStoreKit). Every async export completes by invoking
/// <see cref="OnComplete"/> exactly once with a GCHandle to the pending <see cref="NativeOperation"/>.
/// </summary>
static unsafe partial class StoreKitBridge
{
    const string Lib = "__Internal";

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_can_make_payments")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool CanMakePayments();

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_get_products", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void GetProducts(string idsJson, nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_purchase", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void Purchase(string productId, string optionsJson, nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_current_entitlements")]
    private static partial void CurrentEntitlements(nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_unfinished")]
    private static partial void Unfinished(nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_finish", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void Finish(string transactionId, nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_sync")]
    private static partial void Sync(nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_show_manage_subscriptions")]
    private static partial void ShowManageSubscriptions(nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_storekit_start_updates")]
    private static partial void StartUpdates(delegate* unmanaged<byte*, void> callback);


    internal static Task<string> GetProductsAsync(string idsJson, CancellationToken cancelToken)
        => Invoke(ctx => GetProducts(idsJson, ctx, &OnComplete), cancelToken);

    internal static Task<string> PurchaseAsync(string productId, string optionsJson, CancellationToken cancelToken)
        => Invoke(ctx => Purchase(productId, optionsJson, ctx, &OnComplete), cancelToken);

    internal static Task<string> CurrentEntitlementsAsync(CancellationToken cancelToken)
        => Invoke(ctx => CurrentEntitlements(ctx, &OnComplete), cancelToken);

    internal static Task<string> UnfinishedAsync(CancellationToken cancelToken)
        => Invoke(ctx => Unfinished(ctx, &OnComplete), cancelToken);

    internal static Task<string> FinishAsync(string transactionId, CancellationToken cancelToken)
        => Invoke(ctx => Finish(transactionId, ctx, &OnComplete), cancelToken);

    internal static Task<string> SyncAsync(CancellationToken cancelToken)
        => Invoke(ctx => Sync(ctx, &OnComplete), cancelToken);

    internal static Task<string> ShowManageSubscriptionsAsync(CancellationToken cancelToken)
        => Invoke(ctx => ShowManageSubscriptions(ctx, &OnComplete), cancelToken);


    static Action<string>? updateHandler;

    internal static void StartUpdates(Action<string> onUpdate)
    {
        updateHandler = onUpdate;
        StartUpdates(&OnUpdate);
    }


    static Task<string> Invoke(Action<nint> call, CancellationToken cancelToken)
    {
        var operation = new NativeOperation(cancelToken);
        var handle = GCHandle.Alloc(operation);
        try
        {
            call(GCHandle.ToIntPtr(handle));
        }
        catch
        {
            handle.Free();
            operation.Dispose();
            throw;
        }
        return operation.Task;
    }


    [UnmanagedCallersOnly]
    static void OnComplete(nint context, byte* result, byte* error)
    {
        var handle = GCHandle.FromIntPtr(context);
        var operation = (NativeOperation)handle.Target!;
        handle.Free();

        // strings are only valid for the duration of this callback - copy now
        var resultJson = result == null ? null : Marshal.PtrToStringUTF8((nint)result);
        var errorJson = error == null ? null : Marshal.PtrToStringUTF8((nint)error);
        operation.Complete(resultJson, errorJson);
    }


    [UnmanagedCallersOnly]
    static void OnUpdate(byte* json)
    {
        if (json == null)
            return;

        var payload = Marshal.PtrToStringUTF8((nint)json)!;
        try
        {
            updateHandler?.Invoke(payload);
        }
        catch
        {
            // exceptions must never unwind into Swift
        }
    }


    sealed class NativeOperation : IDisposable
    {
        readonly TaskCompletionSource<string> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly CancellationTokenRegistration registration;

        public NativeOperation(CancellationToken cancelToken)
        {
            // StoreKit calls cannot be cancelled - the native side still completes later and frees the handle
            if (cancelToken.CanBeCanceled)
                this.registration = cancelToken.Register(() => this.tcs.TrySetCanceled(cancelToken));
        }

        public Task<string> Task => this.tcs.Task;

        public void Complete(string? resultJson, string? errorJson)
        {
            this.Dispose();
            if (errorJson != null)
                this.tcs.TrySetException(StoreKitMapper.ToException(errorJson));
            else
                this.tcs.TrySetResult(resultJson ?? "null");
        }

        public void Dispose() => this.registration.Dispose();
    }
}
