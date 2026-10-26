using System.Runtime.InteropServices;

namespace Shiny.AgeSignals;


/// <summary>
/// P/Invoke surface of the ShinyAgeRange Swift framework (native/ShinyAgeRange). Every async export completes by
/// invoking <see cref="OnComplete"/> exactly once with a GCHandle to the pending <see cref="NativeOperation"/>.
/// </summary>
static unsafe partial class AgeRangeBridge
{
    const string Lib = "__Internal";

    [LibraryImport(Lib, EntryPoint = "shiny_agerange_is_supported")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool IsSupported();

    [LibraryImport(Lib, EntryPoint = "shiny_agerange_eligibility")]
    private static partial void Eligibility(nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_agerange_request")]
    private static partial void Request(int gate1, int gate2, int gate3, nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);

    [LibraryImport(Lib, EntryPoint = "shiny_agerange_significant_update", StringMarshalling = StringMarshalling.Utf8)]
    private static partial void SignificantUpdate(string description, nint context, delegate* unmanaged<nint, byte*, byte*, void> callback);


    internal static Task<NativeResult> EligibilityAsync(CancellationToken cancelToken)
        => Invoke(ctx => Eligibility(ctx, &OnComplete), cancelToken);

    // 0 is "no gate" on the Swift side
    internal static Task<NativeResult> RequestAsync(int gate1, int? gate2, int? gate3, CancellationToken cancelToken)
        => Invoke(ctx => Request(gate1, gate2 ?? 0, gate3 ?? 0, ctx, &OnComplete), cancelToken);

    internal static Task<NativeResult> SignificantUpdateAsync(string description, CancellationToken cancelToken)
        => Invoke(ctx => SignificantUpdate(description, ctx, &OnComplete), cancelToken);


    static Task<NativeResult> Invoke(Action<nint> call, CancellationToken cancelToken)
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
        operation.Complete(new NativeResult(resultJson, errorJson));
    }


    /// <summary>Exactly one of the two is set. Errors are left to the caller - some are statuses, not failures.</summary>
    internal readonly record struct NativeResult(string? Json, string? ErrorJson);


    sealed class NativeOperation : IDisposable
    {
        readonly TaskCompletionSource<NativeResult> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly CancellationTokenRegistration registration;

        public NativeOperation(CancellationToken cancelToken)
        {
            // Declared Age Range calls cannot be cancelled - the native side still completes later and frees the handle
            if (cancelToken.CanBeCanceled)
                this.registration = cancelToken.Register(() => this.tcs.TrySetCanceled(cancelToken));
        }

        public Task<NativeResult> Task => this.tcs.Task;

        public void Complete(NativeResult result)
        {
            this.Dispose();
            this.tcs.TrySetResult(result);
        }

        public void Dispose() => this.registration.Dispose();
    }
}
